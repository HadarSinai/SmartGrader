using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Linq;
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Infrastructure.Services.Feedback
{
    public class OpenAiFeedbackService : IFeedbackService
    {
        private const string Url = "https://api.openai.com/v1/chat/completions";
        private const int MaxFailedTestsInPrompt = 5;
        private const int MaxPassedTestsInPrompt = 3;
        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _options;

        public OpenAiFeedbackService(HttpClient httpClient, IOptions<OpenAiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<AiFeedbackResult> GetFeedbackAsync(
            string assignmentDescription,
            string sourceCode,
            int passedTests,
            int totalTests,
            IReadOnlyList<TestCaseResult> testDetails,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                return RawFallback("שגיאה: חסר OpenAi:ApiKey בקונפיגורציה.");

            if (string.IsNullOrWhiteSpace(_options.Model))
                return RawFallback("שגיאה: חסר OpenAi:Model בקונפיגורציה.");

            if (totalTests < 0 || passedTests < 0 || passedTests > totalTests)
                return RawFallback("שגיאה: נתוני הטסטים לא תקינים (passed/total).");

            // הערה: התבנית מתארת טיפוסים (<number>) ולא ערכים לדוגמה. גרסה קודמת כתבה כאן
            // אפסים ממשיים, והמודל העתיק אותם כלשונם — כל תלמיד קיבל איכות קוד 0 ויעילות 0.
            var developerPrompt =
@"You are a C# teacher reviewing a student's solution. The reader is the student.
Language: Hebrew (write ALL strings in Hebrew, gender-neutral phrasing).
Rules:
- Do NOT invent results or errors. The test results below are the only facts about what ran.
- Be concise but include ALL real issues.
- ""good"" must always contain at least one genuine positive observation, even when every test
  failed (a correct loop, sensible naming, right use of a language feature). Never return it empty.
- ""minimal_changes"": the smallest concrete edits that make the code pass. Not vague advice.
- ""optional_full_solution"": only when the minimal changes cannot convey the fix on their own.

Return STRICT JSON only. The values below are TYPE placeholders, not default values:
{ ""good"":[<string>], ""issues"":{""correctness"":[<string>], ""readability"":[<string>], ""performance"":[<string>]},
  ""minimal_changes"":[<string>], ""optional_full_solution"":<string|null>,
  ""scores"":{""test_score"":<number|null>, ""code_quality_score"":<number>, ""efficiency_score"":<number>, ""final_score"":<number>} }

Scoring — every score is on a 0-100 scale, and the three component scores are INDEPENDENT:
- test_score: total>0 ? (passed/total)*100 : null. Compute it from the numbers given, do not guess.
- code_quality_score: naming, structure and readability ONLY. Code that fails every test can still
  score 80 here if it is written well. Never copy test_score into this field.
  Anchors: 90+ clean and idiomatic, 70 readable with minor issues, 50 works but messy, 30 hard to follow.
- efficiency_score: algorithmic complexity and wasted work ONLY. Same independence rule.
  Anchors: 90+ optimal for the task, 70 reasonable, 50 needlessly heavy, 30 badly inefficient.
- final_score = 0.7*test_score + 0.2*code_quality_score + 0.1*efficiency_score, rounded to a whole
  number. When test_score is null, set final_score to null as well rather than inventing one.";

            var testsSummary = BuildTestsSummary(testDetails);

            var userContent =
$@"Task: {assignmentDescription}
Tests: {passedTests} passed out of {totalTests}
{testsSummary}
Code:
{sourceCode}";

            var requestBody = new
            {
                model = _options.Model,
                messages = new object[]
                {
                    new { role = "developer", content = developerPrompt },
                    new { role = "user", content = userContent }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            // Retry על עומס זמני בלבד (429/503) עם backoff
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                using var request = new HttpRequestMessage(HttpMethod.Post, Url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, ct);
                var responseJson = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    string content;
                    try
                    {
                        using var doc = JsonDocument.Parse(responseJson);
                        content = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString() ?? string.Empty;
                    }
                    catch (JsonException)
                    {
                        return RawFallback(responseJson);
                    }

                    return ParseFeedback(content);
                }

                // Retry רק על עומס זמני
                var code = (int)response.StatusCode;
                var isRetryable = response.StatusCode == (HttpStatusCode)429 || response.StatusCode == HttpStatusCode.ServiceUnavailable;

                if (isRetryable && attempt < maxAttempts)
                {
                    var delay = GetRetryDelay(response, attempt);
                    await Task.Delay(delay, ct);
                    continue;
                }

                // שגיאה אחרת / נגמרו ניסיונות
                return RawFallback($"שגיאת AI ({code}): {responseJson}");
            }

            return RawFallback("שגיאה: OpenAI לא זמין כרגע. נסי שוב בעוד כמה רגעים.");
        }

        // מעביר ל-AI גם את הטסטים שעברו ולא רק את הנכשלים: בלעדיהם למודל אין שום עובדה
        // חיובית להיאחז בה, וטאב "מה טוב" יוצא ריק גם כשחלק מהמטלה כן עבד.
        //
        // ⚠️ מקרה בדיקה שאינו דוגמה נכנס לפרומפט בלי הקלט והפלט הצפוי שלו. המשוב של ה-AI מוצג
        // לתלמידה כטקסט חופשי, כך שכל ערך שנכנס לכאן עלול לצאת אליה — זהו נתיב דלף זהה בפועל
        // להחזרת ה-Tests ב-API, רק עקיף. הספירה (עברו/נכשלו) נשמרת, וזה מספיק כדי לבסס את המשוב.
        private static string BuildTestsSummary(IReadOnlyList<TestCaseResult> testDetails)
        {
            if (testDetails.Count == 0)
                return "";

            var sb = new StringBuilder("Test results (grounding facts — do not invent others):\n");

            var passed = testDetails.Where(t => t.Passed).Take(MaxPassedTestsInPrompt).ToList();
            if (passed.Count > 0)
            {
                sb.AppendLine("Passed:");
                foreach (var (t, i) in passed.Select((t, i) => (t, i)))
                    sb.AppendLine(t.IsSample
                        ? $"  {i + 1}. Input: {t.Input} | Expected: {t.Expected}"
                        : $"  {i + 1}. (hidden test — do not mention or guess its input/expected output)");
            }

            var failed = testDetails.Where(t => !t.Passed).Take(MaxFailedTestsInPrompt).ToList();
            if (failed.Count > 0)
            {
                sb.AppendLine("Failed:");
                foreach (var (t, i) in failed.Select((t, i) => (t, i)))
                    sb.AppendLine(t.IsSample
                        ? $"  {i + 1}. Input: {t.Input} | Expected: {t.Expected} | Actual: {t.Actual}" +
                          (string.IsNullOrWhiteSpace(t.Error) ? "" : $" | Error: {t.Error}")
                        : $"  {i + 1}. (hidden test — failed; do not mention or guess its input/expected output)");
            }

            return sb.ToString();
        }

        // מנסה לפרש את תשובת ה-AI כ-JSON מובנה. בכשל — לא זורק, אלא מחזיר תוצאה עם
        // ParseSucceeded=false ותוכן גולמי לגיבוי, בהתאם לאופי החוסן הקיים (היום כל מחרוזת מתקבלת).
        private static AiFeedbackResult ParseFeedback(string content)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<AiFeedbackResult>(content);
                if (parsed is null)
                    return RawFallback(content);

                return parsed with { ParseSucceeded = true };
            }
            catch (JsonException)
            {
                return RawFallback(content);
            }
        }

        private static AiFeedbackResult RawFallback(string rawText) =>
            new(Good: null, Issues: null, MinimalChanges: null, OptionalFullSolution: null, Scores: null)
            {
                ParseSucceeded = false,
                RawResponse = rawText,
            };

        private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            // אם השרת שלח Retry-After (בשניות) - נכבד
            if (response.Headers.TryGetValues("Retry-After", out var values) &&
                int.TryParse(values.FirstOrDefault(), out var retryAfterSeconds) &&
                retryAfterSeconds > 0)
            {
                // גבול קטן כדי לא להמתין יותר מדי
                return TimeSpan.FromSeconds(Math.Min(retryAfterSeconds, 20));
            }

            // backoff: 2s, 4s, 8s
            var seconds = Math.Pow(2, attempt);
            return TimeSpan.FromSeconds(Math.Min(seconds, 10));
        }
    }
}

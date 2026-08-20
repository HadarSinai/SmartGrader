using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartGrader.Application.Services.CodeAnalysis;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Infrastructure.Services.Feedback
{
    /// <summary>
    /// המשוב המילולי מ-OpenAI. ר' <see cref="IFeedbackService"/> לחלוקת התפקידים —
    /// כאן נכתבות רק מילים, אף פעם לא מספרים.
    /// </summary>
    public class OpenAiFeedbackService : IFeedbackService
    {
        private const string Url = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// תקרת פלט. הפרומפט הקודם רץ בלי תקרה בכלל, וגם בלי השדה
        /// <c>optional_full_solution</c> שנמחק — הוא היה הפלט היקר ביותר, והוא מסר לתלמידה
        /// את הפתרון המלא.
        /// </summary>
        private const int MaxOutputTokens = 600;

        /// <summary>
        /// טמפרטורה נמוכה: גם מייצבת את הניסוח בין הרצות של אותו קוד, וגם מצמצמת נטייה
        /// להמציא ממצאים שלא נמסרו.
        /// </summary>
        private const double Temperature = 0.2;

        /// <summary>גזימת המקור. בלי חסם, קובץ שהודבק כולו נשלח כמו שהוא בכל ניסיון.</summary>
        private const int MaxSourceChars = 4000;

        private const int MaxSampleTestsInPrompt = 4;

        /// <summary>
        /// 🔴 <b>לשון נקבה היא דרישה קשיחה.</b> ברירת המחדל של מודל שפה בעברית היא לשון זכר,
        /// ולכן ההוראה חייבת להיות מפורשת. בית ספר לבנות.
        /// </summary>
        private const string SystemPreamble =
@"You are a C# teacher writing feedback for a 9th-grade student in Israel.
Write all text in Hebrew, addressing the student in the FEMININE form
(את, שלך, כתבת, נסי) — this is a girls' school. Warm but direct.
State only the facts given below. Never invent errors or results.
Never state or guess a grade, a score or a number of points.
Return strict JSON only, in this shape (the values are TYPE placeholders, not defaults):
{ ""good"":[<string>], ""issues"":{""correctness"":[<string>], ""readability"":[<string>], ""performance"":[<string>]},
  ""minimal_changes"":[<string>] }
""good"" must always contain at least one genuine positive observation.
""minimal_changes"" are the smallest concrete edits that fix the problem — never the full solution.";

        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _options;

        public OpenAiFeedbackService(HttpClient httpClient, IOptions<OpenAiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        // ── תרחיש 1: שגיאת קומפילציה ──────────────────────────────────────────

        public Task<AiFeedbackResult> GetCompileErrorFeedbackAsync(
            string assignmentDescription,
            string sourceCode,
            string compilerMessage,
            CancellationToken ct)
        {
            // ⚠️ המהדר הוא Mono (Judge0, language_id 51) והוא ישן בהרבה מהמהדר שמנתח את הקוד
            // בשרת. תלמידה יכולה לכתוב switch expression — Roslyn מזהה אותו, "חובה switch"
            // מתקיים, ו-Mono מסרב להדר. בלי ההוראה הזו המודל מסביר שגיאה שאין לה שם, והתלמידה
            // מוחה בצדק "אבל השתמשתי ב-switch!".
            var scenario =
$@"The student's code failed to COMPILE. No tests were run.

The compiler is Mono (an older C# compiler, roughly C# 5-era). If the error is caused by
modern C# syntax that Mono does not support (switch expressions, string interpolation with
$, records, pattern matching, target-typed new, System.Text.Json), say so EXPLICITLY by name
and offer the older equivalent — otherwise the student cannot understand why correct code
was rejected.

Compiler output:
{compilerMessage}

Code:
{Truncate(sourceCode)}";

            return SendAsync(
                assignmentDescription,
                scenario,
                fallback: () => AiFeedbackResult.Deterministic(new[]
                {
                    "❌ הקוד לא עבר הידור.",
                    compilerMessage
                }),
                ct);
        }

        // ── תרחיש 2: דרישה חוסמת שלא התקיימה ──────────────────────────────────

        public Task<AiFeedbackResult> GetRequirementFeedbackAsync(
            string assignmentDescription,
            string sourceCode,
            IReadOnlyList<StructuralRuleResult> failedRules,
            CancellationToken ct)
        {
            var findings = failedRules.Select(StructuralRuleDescriber.DescribeFailure).ToList();

            // אין כאן ולו שורת נתוני טסט אחת: Judge0 לא רץ במסלול הזה, ואין מה למסור.
            var scenario =
$@"The student did not meet a MANDATORY structural requirement of the assignment, so the
solution was not run at all and receives NO grade. This is not a low grade — it is a
""do it again the way it was asked"" situation. Be encouraging: if the logic is sound, say so,
then explain how to convert it to the required construct.

These deterministic findings come from a C# syntax analyser and are the only facts you have:
{string.Join("\n", findings)}

Code:
{Truncate(sourceCode)}";

            return SendAsync(
                assignmentDescription,
                scenario,
                fallback: () => AiFeedbackResult.Deterministic(findings),
                ct);
        }

        // ── תרחיש 3: המסלול הרגיל ─────────────────────────────────────────────

        public Task<AiFeedbackResult> GetGradingFeedbackAsync(
            string assignmentDescription,
            string sourceCode,
            int passedTests,
            int totalTests,
            IReadOnlyList<TestCaseResult> testDetails,
            IReadOnlyList<StructuralRuleResult> ruleResults,
            CancellationToken ct)
        {
            var scenario =
$@"The student's code compiled and ran.
Tests: {passedTests} passed out of {totalTests}.
{BuildSampleTestsSection(testDetails)}{BuildRulesSection(ruleResults)}
Code:
{Truncate(sourceCode)}";

            return SendAsync(
                assignmentDescription,
                scenario,
                fallback: () => AiFeedbackResult.Deterministic(
                    new[] { $"עברו {passedTests} מתוך {totalTests} מקרי בדיקה." }
                        .Concat(ruleResults
                            .Where(r => !r.Passed && r.Rule.Severity != RuleSeverity.Advisory)
                            .Select(StructuralRuleDescriber.DescribeFailure))),
                ct);
        }

        /// <summary>
        /// 🔴 <b>רק מקרי דוגמה נמסרים למודל.</b> מקרה מוסתר נמסר כעובדה בלבד ("נכשל"), בלי
        /// הקלט ובלי הפלט הצפוי — מודל שקיבל אותם יצטט אותם חזרה במשוב, וכל ההסתרה נשברת.
        /// זה מספיק כדי לאתר את הבאג כמעט תמיד, וגם עולה פחות טוקנים.
        /// </summary>
        private static string BuildSampleTestsSection(IReadOnlyList<TestCaseResult> testDetails)
        {
            if (testDetails.Count == 0)
                return "";

            var sb = new StringBuilder();
            var samples = testDetails.Where(t => t.IsSample).Take(MaxSampleTestsInPrompt).ToList();

            if (samples.Count > 0)
            {
                sb.AppendLine("Sample tests (the student can see these — you may quote them):");
                foreach (var (test, i) in samples.Select((t, i) => (t, i)))
                    sb.AppendLine(
                        $"  {i + 1}. Input: {test.Input} | Expected: {test.Expected} | " +
                        $"Actual: {test.Actual} | {(test.Passed ? "PASSED" : "FAILED")}" +
                        (string.IsNullOrWhiteSpace(test.Error) ? "" : $" | Error: {test.Error}"));
            }

            var hiddenFailed = testDetails.Count(t => !t.IsSample && !t.Passed);
            if (hiddenFailed > 0)
                sb.AppendLine(
                    $"{hiddenFailed} HIDDEN test(s) failed. You are NOT given their input or expected " +
                    "output. Never state, guess or hint at an expected value for them — describe the " +
                    "KIND of case the student may have missed instead (e.g. zero, negatives, empty input).");

            return sb.ToString();
        }

        private static string BuildRulesSection(IReadOnlyList<StructuralRuleResult> ruleResults)
        {
            if (ruleResults.Count == 0)
                return "";

            var sb = new StringBuilder("Structural requirements (checked by a syntax analyser, not by you):\n");

            foreach (var result in ruleResults)
                sb.AppendLine(
                    $"  - {StructuralRuleDescriber.Describe(result.Rule)} — " +
                    $"{(result.Passed ? "MET" : "NOT MET")}: {StructuralRuleDescriber.DescribeFinding(result)}");

            return sb.ToString();
        }

        // ── שליחה משותפת ──────────────────────────────────────────────────────

        private async Task<AiFeedbackResult> SendAsync(
            string assignmentDescription,
            string scenario,
            Func<AiFeedbackResult> fallback,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.Model))
                return fallback();

            var requestBody = new
            {
                model = _options.Model,
                max_tokens = MaxOutputTokens,
                temperature = Temperature,
                // מצב JSON: פחות פתיח מילולי לפני האובייקט, ולכן גם פחות כשלי פירוש.
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = SystemPreamble },
                    new { role = "user", content = $"Task: {assignmentDescription}\n\n{scenario}" }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            // Retry על עומס זמני בלבד (429/503) עם backoff
            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
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
                        return fallback();
                    }

                    return ParseFeedback(content, fallback);
                }

                var isRetryable = response.StatusCode is (HttpStatusCode)429 or HttpStatusCode.ServiceUnavailable;

                if (isRetryable && attempt < maxAttempts)
                {
                    await Task.Delay(GetRetryDelay(response, attempt), ct);
                    continue;
                }

                return fallback();
            }

            return fallback();
        }

        /// <summary>
        /// מפרש את תשובת המודל. בכשל אינו זורק אלא חוזר לממצא הדטרמיניסטי — העובדות כבר
        /// נקבעו, וכשל בניסוח אסור שישאיר את התלמידה עם סטטוס חשוף בלי הסבר.
        /// </summary>
        private static AiFeedbackResult ParseFeedback(string content, Func<AiFeedbackResult> fallback)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<AiFeedbackResult>(content);
                return parsed is null
                    ? fallback()
                    : parsed with { ParseSucceeded = true };
            }
            catch (JsonException)
            {
                // הטקסט הגולמי עדיף על הממצא היבש כשהוא בכל זאת הגיע — הוא בעברית ומסביר.
                return new AiFeedbackResult(Good: null, Issues: null, MinimalChanges: null)
                {
                    ParseSucceeded = false,
                    RawResponse = content
                };
            }
        }

        private static string Truncate(string? sourceCode)
        {
            if (string.IsNullOrEmpty(sourceCode))
                return "(no code)";

            return sourceCode.Length <= MaxSourceChars
                ? sourceCode
                : sourceCode[..MaxSourceChars] + "\n… (הקוד נחתך כאן)";
        }

        private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            // אם השרת שלח Retry-After (בשניות) - נכבד
            if (response.Headers.TryGetValues("Retry-After", out var values) &&
                int.TryParse(values.FirstOrDefault(), out var retryAfterSeconds) &&
                retryAfterSeconds > 0)
            {
                return TimeSpan.FromSeconds(Math.Min(retryAfterSeconds, 20));
            }

            // backoff: 2s, 4s, 8s
            return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 10));
        }
    }
}

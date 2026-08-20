using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Infrastructure.Services.Feedback
{
    /// <summary>
    /// מבקש מ-OpenAI מקרי בדיקה מוצעים. השירות הזה מחזיר <b>מועמדים בלבד</b> — האימות
    /// (הרצה מול הפתרון של המורה) קורה ב-<c>SuggestTestCasesHandler</c>, ואין להסתמך כאן
    /// על נכונות אף ערך.
    /// </summary>
    public class OpenAiTestCaseSuggestionService : ITestCaseSuggestionService
    {
        private const string Url = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// תקרת פלט. הפרומפט מבקש JSON קצר ותו לא, וללא תקרה תשובה שהשתבשה עלולה לרוץ
        /// לאורך שהמורה משלמת עליו בלי לקבל דבר.
        /// </summary>
        private const int MaxTokens = 1200;

        /// <summary>
        /// טמפרטורה נמוכה ולא אפס: המשימה כאן היא לגוון קלטים (רגיל, גבול, חריג), וב-0
        /// המודל נוטה להציע חמש וריאציות של אותו מקרה עצמו.
        /// </summary>
        private const double Temperature = 0.3;

        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _options;

        public OpenAiTestCaseSuggestionService(HttpClient httpClient, IOptions<OpenAiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<SuggestedTestCase>> SuggestAsync(
            string description,
            GradingMode gradingMode,
            string? methodName,
            int count,
            CancellationToken ct)
        {
            // כשל תצורה מדווח כשירות לא זמין ולא כרשימה ריקה — "אין הצעות" ו"אין מפתח"
            // הם שני מצבים שונים לגמרי מבחינת המורה.
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new TestCaseSuggestionUnavailableException(
                    "הצעת מקרי בדיקה אינה זמינה — לא מוגדר מפתח OpenAI. אפשר להוסיף מקרים ידנית ולבדוק אותם מול הפתרון לדוגמה.");

            if (string.IsNullOrWhiteSpace(_options.Model))
                throw new TestCaseSuggestionUnavailableException(
                    "הצעת מקרי בדיקה אינה זמינה — לא מוגדר מודל OpenAI.");

            var requestBody = new
            {
                model = _options.Model,
                max_completion_tokens = MaxTokens,
                temperature = Temperature,
                // מכריח JSON תקין ברמת ה-API. בלי זה המודל עוטף את התשובה ב-```json```
                // ובטקסט מלווה, וה-Deserialize נכשל על תשובה שתוכנה היה בסדר גמור.
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "developer", content = BuildDeveloperPrompt() },
                    new { role = "user", content = BuildUserPrompt(description, gradingMode, methodName, count) },
                },
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, Url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // ⚠️ אין retry כאן, בניגוד ל-OpenAiFeedbackService. שם מדובר בעבודת רקע שאיש
                // לא ממתין לה; כאן המורה עומדת מול המסך, והמתנה של 2+4+8 שניות גרועה יותר
                // מכישלון מהיר שאפשר ללחוץ עליו שוב.
                throw new TestCaseSuggestionUnavailableException(
                    "לא הצלחנו להתחבר לשירות ההצעות. אפשר לנסות שוב בעוד רגע, או להוסיף מקרי בדיקה ידנית.");
            }

            using (response)
            {
                var responseJson = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                    throw new TestCaseSuggestionUnavailableException(
                        BuildErrorMessage(response.StatusCode));

                return ParseSuggestions(ExtractContent(responseJson), count);
            }
        }

        private static string BuildErrorMessage(HttpStatusCode statusCode) => statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "מפתח ה-OpenAI אינו תקף, ולכן אי אפשר להציע מקרי בדיקה כרגע.",
            (HttpStatusCode)429 =>
                "שירות ההצעות עמוס כרגע. אפשר לנסות שוב בעוד כמה רגעים.",
            _ =>
                "שירות ההצעות אינו זמין כרגע. כתיבה ידנית של מקרי בדיקה ובדיקתם מול הפתרון לדוגמה ממשיכות לעבוד.",
        };

        // ⚠️ עבודה אחת, פלט תחום, בלי מרחב לפרשנות. במיוחד: אסור למודל להסביר את עצמו
        // מחוץ ל-JSON — ה-response_format חוסם את זה ברמת ה-API, וההנחיה חוסמת גם ניסיון.
        private static string BuildDeveloperPrompt() =>
@"You propose test cases for a C# programming exercise. The reader is the teacher who wrote it.

Rules:
- Cover the ordinary case, boundary values, and any edge case the description implies.
- Inputs MUST match the grading mode's format exactly (described in the user message).
- ""expected"" is your best computation of the correct output. It will be executed against the
  teacher's reference solution and overwritten if you are wrong, so do not guess wildly — but
  never refuse to answer because you are unsure.
- ""why"" is one short Hebrew sentence explaining what the case tests. Gender-neutral phrasing.
- ""core"": true when the case tests the main thing the exercise is about; false when it tests a
  boundary or an unusual input.
- Do not propose two cases that test the same thing.

Return STRICT JSON only, in exactly this shape:
{""cases"":[{""input"":<string>,""expected"":<string>,""why"":<string>,""core"":<boolean>}]}";

        private static string BuildUserPrompt(
            string description, GradingMode gradingMode, string? methodName, int count)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Propose {count} test cases for this C# exercise.");
            sb.AppendLine($"Task: {description}");
            sb.AppendLine($"Grading mode: {gradingMode}");
            sb.AppendLine(InputFormatFor(gradingMode));

            // שם המתודה נותן למודל את מספר וסדר הפרמטרים — בלעדיו במצב Method הוא מנחש
            // כמה ערכים להפריד ברווח.
            if (!string.IsNullOrWhiteSpace(methodName))
                sb.AppendLine($"Method under test: {methodName}");

            return sb.ToString();
        }

        /// <summary>
        /// פורמט הקלט חייב להתאים למסלול ההרצה בפועל (ר' <c>GradingModeRunner</c>): מערך JSON
        /// שנשלח למסלול שמצפה ל-stdin מופרד ברווחים ייכשל בכל מקרה בדיקה, בלי קשר לתוכן.
        /// </summary>
        private static string InputFormatFor(GradingMode gradingMode) => gradingMode switch
        {
            GradingMode.FullProgram =>
                "Input format: the complete stdin the program reads. Use a newline between values that are read by separate Console.ReadLine() calls.",
            GradingMode.MultiFileMethod =>
                "Input format: a JSON array of the entry method's arguments, e.g. [3, 5].",
            _ =>
                "Input format: the method's argument values separated by single spaces, e.g. 3 5.",
        };

        private static string ExtractContent(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or IndexOutOfRangeException)
            {
                throw new TestCaseSuggestionUnavailableException(
                    "התקבלה תשובה לא צפויה משירות ההצעות. אפשר לנסות שוב.");
            }
        }

        private static IReadOnlyList<SuggestedTestCase> ParseSuggestions(string content, int requestedCount)
        {
            SuggestionEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<SuggestionEnvelope>(content);
            }
            catch (JsonException)
            {
                throw new TestCaseSuggestionUnavailableException(
                    "לא הצלחנו לקרוא את ההצעות שהתקבלו. אפשר לנסות שוב.");
            }

            if (envelope?.Cases is not { Count: > 0 })
                return Array.Empty<SuggestedTestCase>();

            return envelope.Cases
                // שורה בלי קלט היא לא מקרה בדיקה. פלט ריק דווקא כן מותר — יש תרגילים
                // שהתשובה הנכונה בהם היא לא להדפיס כלום, והאימות ממילא יכריע.
                .Where(c => c.Input is not null)
                // ⚠️ התקרה נאכפת גם כאן ולא רק בפרומפט: המודל מתעלם מ-n לפעמים, והרשימה
                // הזו נכנסת ישירות למספר הרצות Judge0 — כלומר לעלות ולזמן המתנה.
                .Take(requestedCount)
                .Select(c => new SuggestedTestCase(
                    Input: c.Input!,
                    Expected: c.Expected ?? "",
                    Why: string.IsNullOrWhiteSpace(c.Why) ? null : c.Why,
                    IsCore: c.Core))
                .ToList();
        }

        private sealed class SuggestionEnvelope
        {
            [JsonPropertyName("cases")]
            public List<SuggestionItem>? Cases { get; init; }
        }

        private sealed class SuggestionItem
        {
            [JsonPropertyName("input")]
            public string? Input { get; init; }

            [JsonPropertyName("expected")]
            public string? Expected { get; init; }

            [JsonPropertyName("why")]
            public string? Why { get; init; }

            // ברירת המחדל true תואמת ל-TestCase.IsCore: הרוב הם מקרי ליבה, והמורה מורידה סימון.
            [JsonPropertyName("core")]
            public bool Core { get; init; } = true;
        }
    }
}

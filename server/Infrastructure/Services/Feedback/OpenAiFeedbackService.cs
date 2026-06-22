using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Linq;
using SmartGrader.Application.Services.Feedback;

namespace SmartGrader.Infrastructure.Services.Feedback
{
    public class OpenAiFeedbackService : IFeedbackService
    {
        private const string Url = "https://api.openai.com/v1/chat/completions";
        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _options;

        public OpenAiFeedbackService(HttpClient httpClient, IOptions<OpenAiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GetFeedbackAsync(
            string assignmentDescription,
            string sourceCode,
            int passedTests,
            int totalTests,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                return "שגיאה: חסר OpenAi:ApiKey בקונפיגורציה.";

            if (string.IsNullOrWhiteSpace(_options.Model))
                return "שגיאה: חסר OpenAi:Model בקונפיגורציה.";

            if (totalTests < 0 || passedTests < 0 || passedTests > totalTests)
                return "שגיאה: נתוני הטסטים לא תקינים (passed/total).";

            // Prompt קצר יותר = פחות טוקנים = פחות עלות
//            var developerPrompt =
//@"You are a C# teacher reviewing a student's solution.
//Rules: do NOT invent results/errors; be concise but include ALL issues; minimal fixes; full solution only if needed.
//Return STRICT JSON only:
//{ ""good"":[], ""issues"":{""correctness"":[], ""readability"":[], ""performance"":[]}, ""minimal_changes"":[], ""optional_full_solution"":null,
//  ""scores"":{""test_score"":null, ""code_quality_score"":0, ""efficiency_score"":0, ""final_score"":0} }
//Scoring: test_score = total>0 ? (passed/total)*100 : null; final = 0.7*tests + 0.2*quality + 0.1*efficiency (if test_score is null, explain).";
            var developerPrompt =
@"You are a C# teacher reviewing a student's solution.
Language: Hebrew (write all strings in Hebrew).
Rules: do NOT invent results/errors; be concise but include ALL issues; minimal fixes; full solution only if needed.
Return STRICT JSON only:
{ ""good"":[], ""issues"":{""correctness"":[], ""readability"":[], ""performance"":[]}, ""minimal_changes"":[], ""optional_full_solution"":null,
  ""scores"":{""test_score"":null, ""code_quality_score"":0, ""efficiency_score"":0, ""final_score"":0} }
Scoring: test_score = total>0 ? (passed/total)*100 : null; final = 0.7*tests + 0.2*quality + 0.1*efficiency (if test_score is null, explain).";
            var userContent =
$@"Task: {assignmentDescription}
Tests: {passedTests}/{totalTests}
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
                    using var doc = JsonDocument.Parse(responseJson);
                    return doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? string.Empty;
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
                return $"שגיאת AI ({code}): {responseJson}";
            }

            return "שגיאה: OpenAI לא זמין כרגע. נסי שוב בעוד כמה רגעים.";
        }

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

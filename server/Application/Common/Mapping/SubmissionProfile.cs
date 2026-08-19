using System.Text.Json;
using AutoMapper;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Mapping
{
    public class SubmissionProfile : Profile
    {
        public SubmissionProfile()
        {
            // IsHidden אינו קיים על הישות — הוא נקבע ב-TestVisibility אחרי המיפוי, לפי תפקיד הקורא
            CreateMap<TestCaseResult, TestCaseResultDto>()
                .ForMember(d => d.IsHidden, opt => opt.Ignore());
            CreateMap<SubmissionFile, SubmissionFileDto>();

            CreateMap<Submission, SubmissionResponseDto>()
                .ForMember(d => d.SourceFiles,
                    opt => opt.MapFrom(s => s.SourceFiles))
                .ForMember(d => d.Status,
                    opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.AiError,
                    opt => opt.MapFrom(s => s.AiError))
                .ForMember(d => d.CompileError,
                    opt => opt.MapFrom(s => s.CompileError))
                .ForMember(d => d.TestResults,
                    opt => opt.MapFrom(s => s.TestResults))
                .ForMember(d => d.Feedback,
                    opt => opt.MapFrom(s => ParseFeedback(s.FeedbackJson)))
                .ForMember(d => d.StudentName,
                    opt => opt.MapFrom(s =>
                        s.Student != null ? s.Student.FullName : null))
                .ForMember(d => d.AssignmentName,
                    opt => opt.MapFrom(s =>
                        s.Assignment != null ? s.Assignment.Title : null))
                .ForMember(d => d.LessonId,
                    opt => opt.MapFrom(s =>
                        s.Assignment != null ? s.Assignment.LessonId : 0));
        }

        // ה-JSON הגולמי מגיע מ-OpenAiFeedbackService כבר במבנה AiFeedbackResult (במקרה ההצלחה)
        // או כטקסט חופשי (הגשות ישנות לפני המיגרציה, שהועתקו מ-Comments) — כאן הופך ל-DTO טיפוסי.
        private static AiFeedbackResultDto? ParseFeedback(string? feedbackJson)
        {
            if (string.IsNullOrWhiteSpace(feedbackJson))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(feedbackJson);
                var root = doc.RootElement;

                // JSON תקין אך לא במבנה הצפוי (לדוגמה: מחרוזת טקסט חופשי מוקפת ב-JSON string)
                if (root.ValueKind != JsonValueKind.Object)
                    return RawFallback(feedbackJson);

                return new AiFeedbackResultDto
                {
                    Good = ReadStringList(root, "good"),
                    Issues = ReadIssues(root),
                    MinimalChanges = ReadStringList(root, "minimal_changes"),
                    OptionalFullSolution = ReadNullableString(root, "optional_full_solution"),
                    Scores = ReadScores(root),
                    ParseSucceeded = true,
                };
            }
            catch (JsonException)
            {
                // לא JSON תקין כלל — כנראה טקסט AI גולמי (כולל Comments ישן שהועתק במיגרציה)
                return RawFallback(feedbackJson);
            }
        }

        private static AiFeedbackResultDto RawFallback(string rawText) => new()
        {
            ParseSucceeded = false,
            RawResponse = rawText,
        };

        private static List<string> ReadStringList(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind != JsonValueKind.Array)
                return new List<string>();

            return el.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString() ?? "")
                .ToList();
        }

        private static string? ReadNullableString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var el) && el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : null;
        }

        private static double? ReadNullableDouble(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var el) && el.ValueKind == JsonValueKind.Number
                ? el.GetDouble()
                : null;
        }

        private static AiFeedbackIssuesDto ReadIssues(JsonElement root)
        {
            if (!root.TryGetProperty("issues", out var el) || el.ValueKind != JsonValueKind.Object)
                return new AiFeedbackIssuesDto();

            return new AiFeedbackIssuesDto
            {
                Correctness = ReadStringList(el, "correctness"),
                Readability = ReadStringList(el, "readability"),
                Performance = ReadStringList(el, "performance"),
            };
        }

        private static AiFeedbackScoresDto ReadScores(JsonElement root)
        {
            if (!root.TryGetProperty("scores", out var el) || el.ValueKind != JsonValueKind.Object)
                return new AiFeedbackScoresDto();

            return new AiFeedbackScoresDto
            {
                TestScore = ReadNullableDouble(el, "test_score"),
                CodeQualityScore = ReadNullableDouble(el, "code_quality_score"),
                EfficiencyScore = ReadNullableDouble(el, "efficiency_score"),
                FinalScore = ReadNullableDouble(el, "final_score"),
            };
        }
    }
}

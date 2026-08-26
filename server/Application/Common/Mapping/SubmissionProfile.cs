using System.Text.Json;
using AutoMapper;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Application.Services.CodeAnalysis;
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
            CreateMap<ScoreBreakdown, ScoreBreakdownDto>();

            CreateMap<SubmissionAttempt, SubmissionAttemptDto>()
                .ForMember(d => d.Status, opt => opt.MapFrom(a => a.Status.ToString()));

            // הניסוח העברי נבנה כאן פעם אחת ולא בקליינט: אותו טקסט בדיוק נשלח גם לפרומפט
            // ומשמש גם כמשוב הדטרמיניסטי כשהמודל אינו זמין — ר' StructuralRuleDescriber.
            CreateMap<StructuralRuleResult, StructuralRuleResultDto>()
                .ForMember(d => d.Requirement,
                    opt => opt.MapFrom(r => StructuralRuleDescriber.Describe(r.Rule)))
                .ForMember(d => d.Finding,
                    opt => opt.MapFrom(r => StructuralRuleDescriber.DescribeFinding(r)))
                .ForMember(d => d.Severity,
                    opt => opt.MapFrom(r => r.Rule.Severity.ToString()))
                .ForMember(d => d.Points,
                    opt => opt.MapFrom(r => r.Rule.Severity == RuleSeverity.Scored ? r.Rule.Points : 0))
                .ForMember(d => d.ExpectedCount,
                    opt => opt.MapFrom(r => r.Rule.ExpectedCount));

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
                .ForMember(d => d.StructuralResults,
                    opt => opt.MapFrom(s => s.StructuralResults))
                // מהחדש לישן — הציר נקרא מלמעלה למטה במסך.
                .ForMember(d => d.Attempts,
                    opt => opt.MapFrom(s => s.Attempts.OrderByDescending(a => a.AttemptNumber)))
                .ForMember(d => d.ScoreBreakdown,
                    opt => opt.MapFrom(s => s.ScoreBreakdown))
                // הכלל מערב את סף הציון של התרגיל, ולכן הוא נגזר כאן ולא בקליינט.
                //
                // ⚠️ זה רק חצי מהכלל. נעילת השיעור לתלמידה דורשת שאילתת LessonResult, ו-AutoMapper
                // אינו אסינכרוני — לכן הערך שיוצא מכאן הוא "פתוח לפי הסף", לא "פתוח". את החצי
                // השני מחיל SubmissionLock.ApplyAsync בסוף ה-handler, ובלעדיו ה-DTO משקר לתלמידה.
                // LockReason נשאר null כאן במכוון — ר' SubmissionResponseDto.
                .ForMember(d => d.LockReason, opt => opt.Ignore())
                .ForMember(d => d.CanResubmit,
                    opt => opt.MapFrom(s => s.CanResubmit(
                        s.Assignment != null
                            ? s.Assignment.RetryThreshold
                            : Assignment.DefaultRetryThreshold)))
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
    }
}

using AutoMapper;
using SmartGrader.Domain.Entities;
using SmartGrader.Application.Dtos.Assignments;

namespace SmartGrader.Api.Mapping
{
    public class AssignmentProfile : Profile
    {
        public AssignmentProfile()
        {
            // מיפוי בין TestCase <-> TestCaseDto (בשביל רשימת ה-Tests)
            CreateMap<TestCaseDto, TestCase>().ReverseMap();

            // מיפוי בין ExpectedFile <-> ExpectedFileDto (בשביל רשימת ה-ExpectedFiles)
            CreateMap<ExpectedFileDto, ExpectedFile>().ReverseMap();

            // מיפוי בין ReferenceSolutionFile <-> ReferenceSolutionFileDto (הפתרון לדוגמה)
            CreateMap<ReferenceSolutionFileDto, ReferenceSolutionFile>().ReverseMap();

            // דרישה מבנית <-> DTO. ה-enum-ים עוברים כמחרוזות בדיוק כמו GradingMode:
            // הערך המספרי של CodeConstruct אינו חלק מהחוזה, והקטלוג גדל כל סמסטר.
            CreateMap<StructuralRule, StructuralRuleDto>()
                .ForMember(d => d.Kind, opt => opt.MapFrom(s => s.Kind.ToString()))
                .ForMember(d => d.Construct, opt => opt.MapFrom(s => s.Construct.ToString()))
                .ForMember(d => d.Severity, opt => opt.MapFrom(s => s.Severity.ToString()));

            CreateMap<StructuralRuleDto, StructuralRule>()
                .ForMember(d => d.Kind, opt => opt.MapFrom(s => Enum.Parse<RuleKind>(s.Kind, true)))
                .ForMember(d => d.Construct, opt => opt.MapFrom(s => Enum.Parse<CodeConstruct>(s.Construct, true)))
                .ForMember(d => d.Severity, opt => opt.MapFrom(s => Enum.Parse<RuleSeverity>(s.Severity, true)))
                // דרישה חוסמת היא שער ואינה נושאת ניקוד. בלי האיפוס הזה נקודות שנשלחו בטעות
                // על דרישה חוסמת היו נספרות ב-RulesAllocation ומטות את הרובריקה.
                .ForMember(d => d.Points, opt => opt.MapFrom(
                    s => Enum.Parse<RuleSeverity>(s.Severity, true) == RuleSeverity.Scored ? s.Points : 0));

            // Assignment -> Response (כולל Tests → TestsDto אוטומטית)
            // GradingMode הוא enum בישות ו-string ב-DTO — ממופה במפורש עם ToString().
            CreateMap<Assignment, AssignmentResponseDto>()
                .ForMember(d => d.GradingMode, opt => opt.MapFrom(s => s.GradingMode.ToString()))
                .ForMember(d => d.RulesAllocation,
                    opt => opt.MapFrom(s => s.ScoredRules.Sum(r => r.Points)));

            // Create DTO -> Assignment
            CreateMap<CreateAssignmentRequestDto, Assignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.LessonId, opt => opt.Ignore())   // בא מה-Command
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.TestsJson, opt => opt.Ignore())
                .ForMember(d => d.ExpectedFilesJson, opt => opt.Ignore())
                .ForMember(d => d.ReferenceSolutionJson, opt => opt.Ignore())
                .ForMember(d => d.StructuralRulesJson, opt => opt.Ignore())
                // ⚠️ בניגוד ל-Tests/ExpectedFiles, הפתרון לדוגמה לא ממופה כאן אלא נכתב ב-Handler
                // דרך SetReferenceSolution — שם נזרקות שורות בלי תוכן. מיפוי ישיר לתכונה היה
                // עוקף את הסינון ושומר קובץ ריק, שנשלח ל-Judge0 ומחזיר שגיאת קומפילציה
                // שנראית כאילו הפתרון של המורה שבור.
                .ForMember(d => d.ReferenceSolution, opt => opt.Ignore())
                // ה-Validator מוודא ש-Dto.GradingMode הוא שם enum חוקי לפני שהמיפוי רץ.
                .ForMember(d => d.GradingMode, opt => opt.MapFrom(
                    s => Enum.Parse<GradingMode>(s.GradingMode, true)));
            // ⚠ לא נוגעים ב-Tests/ExpectedFiles: AutoMapper ימפה את הרשימות (TestCaseDto/ExpectedFileDto)
            // זה יקרא ל-set של Tests/ExpectedFiles ויעדכן את TestsJson/ExpectedFilesJson לבד

            // Update DTO -> Assignment (UpdateAssignmentHandler ממפה שדות ידנית ולא קורא ל-IMapper
            // על המסלול הזה — ה-CreateMap כאן נשמר לעקביות/עתידיות בלבד)
            CreateMap<UpdateAssignmentRequestDto, Assignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.LessonId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.TestsJson, opt => opt.Ignore())
                .ForMember(d => d.ExpectedFilesJson, opt => opt.Ignore())
                .ForMember(d => d.ReferenceSolutionJson, opt => opt.Ignore())
                .ForMember(d => d.StructuralRulesJson, opt => opt.Ignore())
                .ForMember(d => d.ReferenceSolution, opt => opt.Ignore())
                .ForMember(d => d.GradingMode, opt => opt.MapFrom(
                    s => Enum.Parse<GradingMode>(s.GradingMode, true)));
        }
    }
}

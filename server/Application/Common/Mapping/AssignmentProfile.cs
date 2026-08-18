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

            // Assignment -> Response (כולל Tests → TestsDto אוטומטית)
            // GradingMode הוא enum בישות ו-string ב-DTO — ממופה במפורש עם ToString().
            CreateMap<Assignment, AssignmentResponseDto>()
                .ForMember(d => d.GradingMode, opt => opt.MapFrom(s => s.GradingMode.ToString()));

            // Create DTO -> Assignment
            CreateMap<CreateAssignmentRequestDto, Assignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.LessonId, opt => opt.Ignore())   // בא מה-Command
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.TestsJson, opt => opt.Ignore())
                .ForMember(d => d.ExpectedFilesJson, opt => opt.Ignore())
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
                .ForMember(d => d.GradingMode, opt => opt.MapFrom(
                    s => Enum.Parse<GradingMode>(s.GradingMode, true)));
        }
    }
}

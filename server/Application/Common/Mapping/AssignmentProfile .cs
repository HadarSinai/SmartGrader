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

            // Assignment -> Response (כולל Tests → TestsDto אוטומטית)
            CreateMap<Assignment, AssignmentResponseDto>();

            // Create DTO -> Assignment
            CreateMap<CreateAssignmentRequestDto, Assignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.LessonId, opt => opt.Ignore())   // בא מה-Command
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.TestsJson, opt => opt.Ignore());
            // ⚠ לא נוגעים ב-Tests: AutoMapper ימפה את List<TestCaseDto> ל-List<TestCase>
            // זה יקרא ל-set של Tests ויעדכן את TestsJson לבד

            // Update DTO -> Assignment
            CreateMap<UpdateAssignmentRequestDto, Assignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.LessonId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.TestsJson, opt => opt.Ignore());
        }
    }
}

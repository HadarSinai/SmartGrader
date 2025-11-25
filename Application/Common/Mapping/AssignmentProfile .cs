using AutoMapper;
using SmartGrader.Domain.Entities;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Application.UseCases.Lessons.UpdateLesson;
using SmartGrader.Application.Dtos.Assignments;


namespace SmartGrader.Api.Mapping
{
    public class AssignmentProfile : Profile
    {
        public AssignmentProfile()
        {
          
            
            CreateMap<Assignment, AssignmentResponseDto>();
            CreateMap<CreateAssignmentRequestDto, Assignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore());

            CreateMap<UpdateAssignmentRequestDto, Assignment>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.LessonId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore());
        }
    }
}

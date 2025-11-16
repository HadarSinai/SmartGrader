using AutoMapper;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Domain.Entities;
using Api.Dtos.LessonResults;
using SmartGrader.Api.Dtos.Lessons;


namespace SmartGrader.Api.Mapping
{
    public class LessonProfile : Profile
    {
        public LessonProfile()
        {
            CreateMap<CreateLessonRequestDto, Lesson>();
            CreateMap<UpdateLessonRequestDto, Lesson>();

            CreateMap<Lesson, LessonResponseDto>()
                .ForMember(d => d.AssignmentsCount,
                    opt => opt.MapFrom(s => s.Assignments != null ? s.Assignments.Count : 0));
        }
    }
}

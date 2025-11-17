using AutoMapper;
using SmartGrader.Domain.Entities;
using SmartGrader.Api.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Application.UseCases.Lessons.UpdateLesson;


namespace SmartGrader.Api.Mapping
{
    public class LessonProfile : Profile
    {
        public LessonProfile()
        {
            CreateMap<CreateLessonRequestDto, CreateLessonCommand>();
            CreateMap<UpdateLessonRequestDto, UpdateLessonCommand>();

            CreateMap<Lesson, LessonResponseDto>()
                .ForMember(d => d.AssignmentsCount,
                    opt => opt.MapFrom(s => s.Assignments != null ? s.Assignments.Count : 0));
        }
    }
}

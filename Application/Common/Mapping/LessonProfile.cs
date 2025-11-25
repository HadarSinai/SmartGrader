using SmartGrader.Domain.Entities;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Application.UseCases.Lessons.UpdateLesson;
using AutoMapper;

namespace Application.Common.Mapping
{
    public class LessonProfile : Profile
    {
        public LessonProfile()
        {
            CreateMap<Lesson, LessonResponseDto>()
                 .ForMember(d => d.AssignmentsCount,
                     opt => opt.MapFrom(s => s.Assignments != null ? s.Assignments.Count : 0));

            CreateMap<CreateLessonRequestDto, Lesson>();

            CreateMap<UpdateLessonRequestDto, Lesson>()
                .ForMember(d => d.Id, opt => opt.Ignore());

        }
    }
}

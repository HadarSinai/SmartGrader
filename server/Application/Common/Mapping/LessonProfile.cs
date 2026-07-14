using SmartGrader.Domain.Entities;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.Common.HebrewDate;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Application.UseCases.Lessons.UpdateLesson;
using AutoMapper;

namespace SmartGrader.Application.Common.Mapping
{
    public class LessonProfile : Profile
    {
        public LessonProfile()
        {
            CreateMap<Lesson, LessonResponseDto>()
                 .ForMember(d => d.AssignmentsCount,
                     opt => opt.MapFrom(s => s.Assignments != null ? s.Assignments.Count : 0))
                 .ForMember(d => d.LessonDateHebrew,
                     opt => opt.MapFrom(s => HebrewDateConverter.ToHebrewString(s.LessonDate)))
                 .ForMember(d => d.HebrewYear,
                     opt => opt.MapFrom(s => HebrewDateConverter.GetHebrewParts(s.LessonDate).Year))
                 .ForMember(d => d.HebrewMonth,
                     opt => opt.MapFrom(s => HebrewDateConverter.GetHebrewParts(s.LessonDate).Month))
                 .ForMember(d => d.HebrewDay,
                     opt => opt.MapFrom(s => HebrewDateConverter.GetHebrewParts(s.LessonDate).Day));

            CreateMap<CreateLessonRequestDto, Lesson>()
                .ForMember(d => d.LessonDate,
                    opt => opt.MapFrom(s => HebrewDateConverter.ToGregorian(s.HebrewYear, s.HebrewMonth, s.HebrewDay)));

            CreateMap<UpdateLessonRequestDto, Lesson>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.LessonDate,
                    opt => opt.MapFrom(s => HebrewDateConverter.ToGregorian(s.HebrewYear, s.HebrewMonth, s.HebrewDay)));

        }
    }
}

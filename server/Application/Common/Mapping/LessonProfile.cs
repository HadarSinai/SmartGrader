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
                     opt => opt.MapFrom(s => HebrewDateConverter.GetHebrewParts(s.LessonDate).Day))
                 .ForMember(d => d.CourseName,
                     opt => opt.MapFrom(s => s.Course != null ? s.Course.Name : string.Empty))
                 .ForMember(d => d.Classes,
                     opt => opt.MapFrom(s => s.Classes))
                 .ForMember(d => d.ClassNames,
                     opt => opt.MapFrom(s => string.Join(", ", s.Classes.Select(c => c.Name))));

            CreateMap<SchoolClass, LessonClassDto>();

            // ClassIds נפתרים ב-handler (טעינת ישויות SchoolClass) — לא במיפוי
            // TeacherId נקבע ב-handler אחרי המיפוי (CreateLessonHandler) — אין CourseId/TeacherId מקבילים ב-DTO
            // הזה חוץ מ-CourseId שממופה אוטומטית (אותו שם).
            CreateMap<CreateLessonRequestDto, Lesson>()
                .ForMember(d => d.Classes, opt => opt.Ignore())
                .ForMember(d => d.LessonDate,
                    opt => opt.MapFrom(s => HebrewDateConverter.ToGregorian(s.HebrewYear, s.HebrewMonth, s.HebrewDay)));

            CreateMap<UpdateLessonRequestDto, Lesson>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Classes, opt => opt.Ignore())
                // ⚠️ RISK R1 — בלי זה, עדכון שיעור מאפס TeacherId ל-0 (ל-DTO אין שדה כזה, אבל
                // ה-Ignore הזה הוא רשת ביטחון קשיחה, לא רק תוצאה של היעדר שדה במקרה זה). ראו
                // גם Id/CreatedAt/Classes מעל — אותו רציונל בדיוק.
                .ForMember(d => d.TeacherId, opt => opt.Ignore())
                .ForMember(d => d.LessonDate,
                    opt => opt.MapFrom(s => HebrewDateConverter.ToGregorian(s.HebrewYear, s.HebrewMonth, s.HebrewDay)));

        }
    }
}

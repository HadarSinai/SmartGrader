using AutoMapper;
using SmartGrader.Application.Dtos.Teacher;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Mapping
{
    public class TeacherProfile : Profile
    {
        public TeacherProfile()
        {
            // ⚠️ אין כאן CreateMap מ-CreateTeacherRequestDto ל-User במכוון: ל-User יש
            // ctor מוגן ו-Create כמפעל יחיד, והסיסמה חייבת לעבור דרך ה-hasher ב-handler.
            //
            // הספירות מגיעות מה-handler ולא מחושבות כאן — ל-User אין ניווט אל Lessons/Courses,
            // והמיפוי לא יכול לרוץ ל-DB.
            CreateMap<User, TeacherResponseDto>()
                .ForCtorParam("LessonsCount", opt => opt.MapFrom(_ => 0))
                .ForCtorParam("CoursesCount", opt => opt.MapFrom(_ => 0));
        }
    }
}

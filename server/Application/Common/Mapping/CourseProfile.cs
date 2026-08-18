using SmartGrader.Domain.Entities;
using SmartGrader.Application.Dtos.Courses;
using AutoMapper;

namespace SmartGrader.Application.Common.Mapping
{
    public class CourseProfile : Profile
    {
        public CourseProfile()
        {
            CreateMap<Course, CourseResponseDto>()
                .ForMember(d => d.LessonsCount,
                    opt => opt.MapFrom(s => s.Lessons != null ? s.Lessons.Count : 0));

            // Course נוצר דרך ה-factory (Course.Create), לא ממופה ישירות מ-DTO — כמו SchoolClass.
        }
    }
}

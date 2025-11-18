using Api.Dtos.Student;
using AutoMapper;
using SmartGrader.Domain.Entities;

namespace Api.Mapping
{
    public class StudentProfile : Profile
    {
        public StudentProfile()
        {
            // Update DTO → Entity
            CreateMap<UpdateStudentRequestDto, Student>();

            // Entity → Response DTO
            CreateMap<Student, StudentResponseDto>()
                .ForMember(dest => dest.SubmissionsCount,
                           opt => opt.MapFrom(src => src.Submissions != null ? src.Submissions.Count : 0))
                .ForMember(dest => dest.LessonResultsCount,
                           opt => opt.MapFrom(src => src.LessonResults != null ? src.LessonResults.Count : 0));
        }
    }
}

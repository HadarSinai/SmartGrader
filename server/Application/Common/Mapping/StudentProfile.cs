using AutoMapper;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Mapping
{
    public class StudentProfile : Profile
    {
        public StudentProfile()
        {
            // Create DTO → Entity
            CreateMap<CreateStudentRequestDto, Student>();

            // Create-with-account DTO → Entity (auth flow — only academic fields)
            CreateMap<CreateStudentAccountRequestDto, Student>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            // Update DTO → Entity
            CreateMap<UpdateStudentRequestDto, Student>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Submissions, opt => opt.Ignore())
                .ForMember(dest => dest.LessonResults, opt => opt.Ignore());

            // Entity → Response DTO
            CreateMap<Student, StudentResponseDto>()
                .ForMember(dest => dest.SubmissionsCount,
                           opt => opt.MapFrom(src => src.Submissions != null ? src.Submissions.Count : 0))
                .ForMember(dest => dest.LessonResultsCount,
                           opt => opt.MapFrom(src => src.LessonResults != null ? src.LessonResults.Count : 0))
                .ForMember(dest => dest.HasAccount,
                           opt => opt.MapFrom(src => src.UserId != null));
        }
    }
}


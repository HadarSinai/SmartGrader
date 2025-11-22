using AutoMapper;
using SmartGrader.Api.Dtos.Submissions;
using SmartGrader.Application.UseCases.Submissions.CreateSubmission;
using SmartGrader.Application.UseCases.Submissions.UpdateSubmission;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Api.Mapping
{
    public class SubmissionProfile : Profile
    {
        public SubmissionProfile()
        {
            // יצירה
            CreateMap<CreateSubmissionRequestDto, CreateSubmissionCommand>();

            // עדכון
            CreateMap<UpdateSubmissionRequestDto, UpdateSubmissionCommand>();

            // תגובה ללקוח
            CreateMap<Submission, SubmissionResponseDto>()
                .ForMember(d => d.StudentName,
                    opt => opt.MapFrom(s => s.Student != null ? s.Student.FullName : null))
                .ForMember(d => d.AssignmentName,
                    opt => opt.MapFrom(s => s.Assignment != null ? s.Assignment.Title : null));
        }
    }
}

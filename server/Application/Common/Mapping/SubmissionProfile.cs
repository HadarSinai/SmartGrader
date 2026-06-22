using AutoMapper;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Mapping
{
    public class SubmissionProfile : Profile
    {
        public SubmissionProfile()
        {
            CreateMap<Submission, SubmissionResponseDto>()
                .ForMember(d => d.Status,
                    opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.AiError,
                    opt => opt.MapFrom(s => s.AiError))
                .ForMember(d => d.StudentName,
                    opt => opt.MapFrom(s =>
                        s.Student != null ? s.Student.FullName : null))
                .ForMember(d => d.AssignmentName,
                    opt => opt.MapFrom(s =>
                        s.Assignment != null ? s.Assignment.Title : null));
        }
    }
}

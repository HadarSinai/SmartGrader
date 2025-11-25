using AutoMapper;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Mapping
{
    public class SubmissionProfile : Profile
    {
        public SubmissionProfile()
        {
            // Create DTO → Entity
            CreateMap<CreateSubmissionRequestDto, Submission>()
                // Score ו-CheckedByAI נקבעים ע"י המערכת
                .ForMember(d => d.Score, opt => opt.MapFrom(_ => 0.0))
                .ForMember(d => d.CheckedByAI, opt => opt.MapFrom(_ => false))
                // SubmittedAt יש לו ברירת מחדל ב-Entity
                .ForMember(d => d.SubmittedAt, opt => opt.Ignore());

            // Update DTO → Entity
            CreateMap<UpdateSubmissionRequestDto, Submission>()
                // לא מאפשרים לשנות שיוך של סטודנט/משימה בעדכון
                .ForMember(d => d.StudentId, opt => opt.Ignore())
                .ForMember(d => d.AssignmentId, opt => opt.Ignore())
                .ForMember(d => d.SubmittedAt, opt => opt.Ignore());

            // Entity → Response DTO
            CreateMap<Submission, SubmissionResponseDto>()
                .ForMember(d => d.StudentName,
                    opt => opt.MapFrom(s =>
                        s.Student != null ? s.Student.FullName : null))
                .ForMember(d => d.AssignmentName,
                    opt => opt.MapFrom(s =>
                        s.Assignment != null ? s.Assignment.Title : null));
        }
    }
}

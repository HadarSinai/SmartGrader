using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissionById
{
    // TeacherId — ר' GetSubmissionsQuery.
    public record GetSubmissionByIdQuery(int StudentId, int SubmissionId, int? TeacherId) : IRequest<SubmissionResponseDto>;
}

using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Submissions.UpdateSubmission
{
    // TeacherId — ר' GetSubmissionsQuery.
    public record UpdateSubmissionCommand(
        int StudentId,
        int SubmissionId,
        UpdateSubmissionRequestDto Dto,
        int? TeacherId
    ) : IRequest<SubmissionResponseDto>;
}

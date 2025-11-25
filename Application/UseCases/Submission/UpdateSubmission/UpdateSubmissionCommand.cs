using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Submissions.UpdateSubmission
{
    public record UpdateSubmissionCommand(
        int StudentId,
        int SubmissionId,
        UpdateSubmissionRequestDto Dto
    ) : IRequest<SubmissionResponseDto>;
}

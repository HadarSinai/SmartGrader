using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Submissions.CreateSubmission
{
    public record CreateSubmissionCommand(
      int StudentId,  CreateSubmissionRequestDto Dto
    ) : IRequest<SubmissionResponseDto>;
}

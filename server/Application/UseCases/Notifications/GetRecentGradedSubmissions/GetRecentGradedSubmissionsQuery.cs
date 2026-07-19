using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions
{
    public record GetRecentGradedSubmissionsQuery(int Limit = 20)
        : IRequest<IReadOnlyList<SubmissionResponseDto>>;
}

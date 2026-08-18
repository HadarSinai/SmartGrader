using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions
{
    public record GetRecentGradedSubmissionsQuery(int? TeacherId, int? StudentId, int Limit = 20)
        : IRequest<IReadOnlyList<SubmissionResponseDto>>;
}

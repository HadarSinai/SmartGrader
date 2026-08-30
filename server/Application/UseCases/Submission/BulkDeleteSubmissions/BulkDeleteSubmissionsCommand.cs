using MediatR;
using SmartGrader.Application.Dtos.Common;

namespace SmartGrader.Application.UseCases.Submissions.BulkDeleteSubmissions
{
    /// <param name="TeacherId">בעלות על השיעור — <c>OwnerScopeTeacherId</c>. <c>null</c> = מנהל/ת.</param>
    public record BulkDeleteSubmissionsCommand(
        int StudentId,
        IReadOnlyList<int> SubmissionIds,
        int? TeacherId) : IRequest<BulkDeleteResultDto>;
}

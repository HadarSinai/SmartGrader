using MediatR;
using SmartGrader.Application.Dtos.Common;

namespace SmartGrader.Application.UseCases.Assignments.BulkDeleteAssignments
{
    /// <param name="TeacherId">בעלות על השיעור — <c>OwnerScopeTeacherId</c>. <c>null</c> = מנהל/ת.</param>
    public record BulkDeleteAssignmentsCommand(
        int LessonId,
        IReadOnlyList<int> AssignmentIds,
        int? TeacherId) : IRequest<BulkDeleteResultDto>;
}

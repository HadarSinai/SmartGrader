using MediatR;
using SmartGrader.Application.Dtos.Common;

namespace SmartGrader.Application.UseCases.Lessons.BulkDeleteLessons
{
    /// <param name="TeacherId">בעלות על השיעור — <c>OwnerScopeTeacherId</c>. <c>null</c> = מנהל/ת.</param>
    public record BulkDeleteLessonsCommand(
        IReadOnlyList<int> LessonIds,
        int? TeacherId) : IRequest<BulkDeleteResultDto>;
}

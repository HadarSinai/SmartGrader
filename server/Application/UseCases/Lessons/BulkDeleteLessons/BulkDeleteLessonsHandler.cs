using MediatR;
using SmartGrader.Application.Common.BulkDelete;
using SmartGrader.Application.Dtos.Common;
using SmartGrader.Application.UseCases.Lessons.DeleteLesson;

namespace SmartGrader.Application.UseCases.Lessons.BulkDeleteLessons
{
    /// <summary>
    /// מוחקת את השיעורים שנבחרו, אחד-אחד, דרך <see cref="DeleteLessonCommand"/> עצמה — כולל
    /// בדיקת הבעלות וכולל החסימה על שיעור שיש בו הגשות או ציונים סופיים.
    /// ר' <see cref="BulkDeleteRunner"/>.
    /// </summary>
    public class BulkDeleteLessonsHandler
        : IRequestHandler<BulkDeleteLessonsCommand, BulkDeleteResultDto>
    {
        private readonly IMediator _mediator;

        public BulkDeleteLessonsHandler(IMediator mediator) => _mediator = mediator;

        public Task<BulkDeleteResultDto> Handle(
            BulkDeleteLessonsCommand request,
            CancellationToken ct) =>
            BulkDeleteRunner.RunAsync(
                request.LessonIds,
                id => _mediator.Send(new DeleteLessonCommand(id, request.TeacherId), ct),
                ct);
    }
}

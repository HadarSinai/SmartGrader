using MediatR;
using SmartGrader.Application.Common.BulkDelete;
using SmartGrader.Application.Dtos.Common;
using SmartGrader.Application.UseCases.Students.DeleteStudent;

namespace SmartGrader.Application.UseCases.Students.BulkDeleteStudents
{
    /// <summary>
    /// מוחקת את התלמידות שנבחרו, אחת-אחת, דרך <see cref="DeleteStudentCommand"/> עצמה —
    /// כולל החסימה על תלמידה שיש לה הגשות או ציונים סופיים, וכולל מחיקת חשבון הכניסה שלה.
    /// ר' <see cref="BulkDeleteRunner"/>.
    /// </summary>
    public class BulkDeleteStudentsHandler
        : IRequestHandler<BulkDeleteStudentsCommand, BulkDeleteResultDto>
    {
        private readonly IMediator _mediator;

        public BulkDeleteStudentsHandler(IMediator mediator) => _mediator = mediator;

        public Task<BulkDeleteResultDto> Handle(
            BulkDeleteStudentsCommand request,
            CancellationToken ct) =>
            BulkDeleteRunner.RunAsync(
                request.StudentIds,
                id => _mediator.Send(new DeleteStudentCommand(id), ct),
                ct);
    }
}

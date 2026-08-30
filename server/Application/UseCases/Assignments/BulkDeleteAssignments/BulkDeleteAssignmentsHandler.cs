using MediatR;
using SmartGrader.Application.Common.BulkDelete;
using SmartGrader.Application.Dtos.Common;
using SmartGrader.Application.UseCases.Assignments.DeleteAssignment;

namespace SmartGrader.Application.UseCases.Assignments.BulkDeleteAssignments
{
    /// <summary>
    /// מוחקת את התרגילים שנבחרו, אחד-אחד, דרך <see cref="DeleteAssignmentCommand"/> עצמה —
    /// כולל בדיקת הבעלות על השיעור וכולל החסימה על תרגיל שיש בו הגשות. ר' <see cref="BulkDeleteRunner"/>.
    /// </summary>
    public class BulkDeleteAssignmentsHandler
        : IRequestHandler<BulkDeleteAssignmentsCommand, BulkDeleteResultDto>
    {
        private readonly IMediator _mediator;

        public BulkDeleteAssignmentsHandler(IMediator mediator) => _mediator = mediator;

        public Task<BulkDeleteResultDto> Handle(
            BulkDeleteAssignmentsCommand request,
            CancellationToken ct) =>
            BulkDeleteRunner.RunAsync(
                request.AssignmentIds,
                id => _mediator.Send(
                    new DeleteAssignmentCommand(request.LessonId, id, request.TeacherId), ct),
                ct);
    }
}

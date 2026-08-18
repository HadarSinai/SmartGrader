using MediatR;

namespace SmartGrader.Application.UseCases.Assignments.DeleteAssignment
{
    public record DeleteAssignmentCommand(int LessonId, int AssignmentId, int? TeacherId)
        : IRequest<Unit>;
}

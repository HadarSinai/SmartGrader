using MediatR;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Assignments.UpdateAssignment
{
    public record UpdateAssignmentCommand(
        int LessonId,
        int AssignmentId,
        UpdateAssignmentRequestDto Dto
    ) : IRequest<AssignmentResponseDto>;
}

using MediatR;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Assignments.CreateAssignment
{
    public record CreateAssignmentCommand(
        int LessonId,
        CreateAssignmentRequestDto Dto
    ) : IRequest<AssignmentResponseDto>;
}

using MediatR;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignmentById
{
    public record GetAssignmentByIdQuery(int LessonId, int AssignmentId)
        : IRequest<AssignmentResponseDto>;
}

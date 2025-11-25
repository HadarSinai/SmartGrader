using MediatR;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignments
{
    public record GetAssignmentsQuery(int LessonId)
        : IRequest<IReadOnlyList<AssignmentResponseDto>>;
}

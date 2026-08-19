using MediatR;
using SmartGrader.Application.Dtos.Assignments;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignmentById
{
    // StudentId — ר' GetLessonByIdQuery.
    public record GetAssignmentByIdQuery(int LessonId, int AssignmentId, int? TeacherId, int? StudentId = null)
        : IRequest<AssignmentResponseDto>;
}

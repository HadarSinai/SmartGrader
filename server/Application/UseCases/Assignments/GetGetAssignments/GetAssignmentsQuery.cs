using MediatR;
using SmartGrader.Application.Dtos.Assignments;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignments
{
    // StudentId — ר' GetLessonByIdQuery: בלעדיו תלמידה קוראת את התרגילים (כולל ה-Tests
    // והפלטים הצפויים) של כל שיעור בבית הספר.
    public record GetAssignmentsQuery(int LessonId, int? TeacherId, int? StudentId = null)
        : IRequest<IReadOnlyList<AssignmentResponseDto>>
    {
        /// <summary>ר' GetAssignmentByIdQuery.IsStudentCaller.</summary>
        public bool IsStudentCaller => StudentId.HasValue;
    }
}

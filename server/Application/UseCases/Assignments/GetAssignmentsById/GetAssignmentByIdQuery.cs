using MediatR;
using SmartGrader.Application.Dtos.Assignments;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignmentById
{
    // StudentId — ר' GetLessonByIdQuery.
    public record GetAssignmentByIdQuery(int LessonId, int AssignmentId, int? TeacherId, int? StudentId = null)
        : IRequest<AssignmentResponseDto>
    {
        /// <summary>
        /// תפקיד הקורא, כפי שנקבע ב-Controller: StudentId מוזרם רק לקורא שאינו מורה/מנהלת
        /// (ר' TryResolveSharedReadScope). ⚠️ אין להסיק "תלמידה" מ-TeacherId is null —
        /// הוא null גם עבור מנהלת. ר' TestVisibility.
        /// </summary>
        public bool IsStudentCaller => StudentId.HasValue;
    }
}

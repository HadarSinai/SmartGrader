using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions
{
    // ⚠️ נתיב דלף שלישי של תוצאות הבדיקה: פעמון ההתראות מחזיר SubmissionResponseDto מלא,
    // ותלמידה קוראת אותו. StudentId מוזרם רק לקורא שאינו מורה/מנהלת — ר' TestVisibility.
    public record GetRecentGradedSubmissionsQuery(int? TeacherId, int? StudentId, int Limit = 20)
        : IRequest<IReadOnlyList<SubmissionResponseDto>>
    {
        public bool IsStudentCaller => StudentId.HasValue;
    }
}

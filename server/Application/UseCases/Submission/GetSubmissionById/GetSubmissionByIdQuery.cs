using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissionById
{
    // TeacherId — ר' GetSubmissionsQuery.
    // IsStudentCaller — תפקיד הקורא, נקבע ב-Controller (!IsPrivilegedUser). ⚠️ אי אפשר לגזור
    // אותו מ-TeacherId is null: זה גם המצב של מנהלת. הוא קובע אם תוצאות של מקרי בדיקה מוסתרים
    // מרוקנות לפני ההחזרה — ר' TestVisibility.
    public record GetSubmissionByIdQuery(int StudentId, int SubmissionId, int? TeacherId, bool IsStudentCaller)
        : IRequest<SubmissionResponseDto>;
}

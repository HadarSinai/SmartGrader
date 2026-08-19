using MediatR;

namespace SmartGrader.Application.UseCases.Submissions.DeleteSubmission
{
    // TeacherId — ר' GetSubmissionsQuery. המחיקה פתוחה למורה/מנהלת בלבד, ולכן חייבת סינון בעלות.
    public record DeleteSubmissionCommand(int StudentId, int SubmissionId, int? TeacherId) : IRequest;
}

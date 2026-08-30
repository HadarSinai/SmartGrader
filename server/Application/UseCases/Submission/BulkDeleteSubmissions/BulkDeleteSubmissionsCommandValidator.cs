using FluentValidation;
using SmartGrader.Application.Common.BulkDelete;

namespace SmartGrader.Application.UseCases.Submissions.BulkDeleteSubmissions
{
    public class BulkDeleteSubmissionsCommandValidator
        : AbstractValidator<BulkDeleteSubmissionsCommand>
    {
        public BulkDeleteSubmissionsCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("Student Id must be greater than 0.");

            // בקשה ריקה אינה "מחיקה שהצליחה על אפס שורות" — היא כפתור שנלחץ בלי בחירה.
            RuleFor(x => x.SubmissionIds)
                .NotEmpty().WithMessage("יש לבחור לפחות שורה אחת למחיקה.")
                .Must(ids => ids.Count <= BulkDeleteRunner.MaxIdsPerRequest)
                .WithMessage(BulkDeleteRunner.TooManyIdsMessage);
        }
    }
}

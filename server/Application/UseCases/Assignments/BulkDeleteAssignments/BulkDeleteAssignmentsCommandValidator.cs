using FluentValidation;
using SmartGrader.Application.Common.BulkDelete;

namespace SmartGrader.Application.UseCases.Assignments.BulkDeleteAssignments
{
    public class BulkDeleteAssignmentsCommandValidator
        : AbstractValidator<BulkDeleteAssignmentsCommand>
    {
        public BulkDeleteAssignmentsCommandValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0).WithMessage("LessonId must be greater than 0");

            // בקשה ריקה אינה "מחיקה שהצליחה על אפס שורות" — היא כפתור שנלחץ בלי בחירה.
            RuleFor(x => x.AssignmentIds)
                .NotEmpty().WithMessage("יש לבחור לפחות שורה אחת למחיקה.")
                .Must(ids => ids.Count <= BulkDeleteRunner.MaxIdsPerRequest)
                .WithMessage(BulkDeleteRunner.TooManyIdsMessage);
        }
    }
}

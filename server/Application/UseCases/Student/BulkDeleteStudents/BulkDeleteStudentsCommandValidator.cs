using FluentValidation;
using SmartGrader.Application.Common.BulkDelete;

namespace SmartGrader.Application.UseCases.Students.BulkDeleteStudents
{
    public class BulkDeleteStudentsCommandValidator : AbstractValidator<BulkDeleteStudentsCommand>
    {
        public BulkDeleteStudentsCommandValidator()
        {
            // בקשה ריקה אינה "מחיקה שהצליחה על אפס שורות" — היא כפתור שנלחץ בלי בחירה.
            RuleFor(x => x.StudentIds)
                .NotEmpty().WithMessage("יש לבחור לפחות שורה אחת למחיקה.")
                .Must(ids => ids.Count <= BulkDeleteRunner.MaxIdsPerRequest)
                .WithMessage(BulkDeleteRunner.TooManyIdsMessage);
        }
    }
}

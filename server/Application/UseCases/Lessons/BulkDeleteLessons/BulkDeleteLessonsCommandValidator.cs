using FluentValidation;
using SmartGrader.Application.Common.BulkDelete;

namespace SmartGrader.Application.UseCases.Lessons.BulkDeleteLessons
{
    public class BulkDeleteLessonsCommandValidator : AbstractValidator<BulkDeleteLessonsCommand>
    {
        public BulkDeleteLessonsCommandValidator()
        {
            // בקשה ריקה אינה "מחיקה שהצליחה על אפס שורות" — היא כפתור שנלחץ בלי בחירה.
            RuleFor(x => x.LessonIds)
                .NotEmpty().WithMessage("יש לבחור לפחות שורה אחת למחיקה.")
                .Must(ids => ids.Count <= BulkDeleteRunner.MaxIdsPerRequest)
                .WithMessage(BulkDeleteRunner.TooManyIdsMessage);
        }
    }
}

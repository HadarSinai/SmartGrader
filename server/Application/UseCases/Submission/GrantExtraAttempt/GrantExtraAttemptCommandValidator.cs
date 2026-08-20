using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.GrantExtraAttempt;

public class GrantExtraAttemptCommandValidator : AbstractValidator<GrantExtraAttemptCommand>
{
    public GrantExtraAttemptCommandValidator()
    {
        RuleFor(x => x.SubmissionId).GreaterThan(0);
        RuleFor(x => x.TeacherUserId).GreaterThan(0);

        // הסיבה היא יומן הביקורת עצמו — היא מה שמחליף את "לראות מי השתמשה בקוד",
        // ולכן היא חובה ולא שדה רשות.
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("נא לציין סיבה לאישור ההגשה הנוספת")
            .MaximumLength(500);
    }
}

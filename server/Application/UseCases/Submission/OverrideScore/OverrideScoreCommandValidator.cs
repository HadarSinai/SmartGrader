using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.OverrideScore;

public class OverrideScoreCommandValidator : AbstractValidator<OverrideScoreCommand>
{
    public OverrideScoreCommandValidator()
    {
        RuleFor(x => x.SubmissionId).GreaterThan(0);
        RuleFor(x => x.TeacherUserId).GreaterThan(0);

        // הגבול העליון תלוי בתרגיל (בונוס עובר 100) ולכן נבדק ב-handler, שיש לו את הישות.
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("נא לציין סיבה לשינוי הציון")
            .MaximumLength(500);
    }
}

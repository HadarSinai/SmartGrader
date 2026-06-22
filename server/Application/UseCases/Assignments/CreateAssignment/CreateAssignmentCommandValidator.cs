using FluentValidation;

namespace SmartGrader.Application.UseCases.Assignments.CreateAssignment
{
    public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
    {
        public CreateAssignmentCommandValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0)
                .WithMessage("LessonId must be greater than 0");

            RuleFor(x => x.Dto.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100);

            RuleFor(x => x.Dto.BonusValue)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Dto.IsBonus);
        }
    }
}

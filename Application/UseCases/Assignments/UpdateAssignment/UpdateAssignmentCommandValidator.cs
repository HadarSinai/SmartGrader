using FluentValidation;

namespace SmartGrader.Application.UseCases.Assignments.UpdateAssignment
{
    public class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
    {
        public UpdateAssignmentCommandValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0);

            RuleFor(x => x.AssignmentId)
                .GreaterThan(0);

            RuleFor(x => x.Dto.Title)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Dto.BonusValue)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Dto.IsBonus);
        }
    }
}

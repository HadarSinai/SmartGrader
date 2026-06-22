using FluentValidation;

namespace SmartGrader.Application.UseCases.Assignments.DeleteAssignment
{
    public class DeleteAssignmentCommandValidator : AbstractValidator<DeleteAssignmentCommand>
    {
        public DeleteAssignmentCommandValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0)
                .WithMessage("Lesson ID must be greater than 0.");

            RuleFor(x => x.AssignmentId)
                .GreaterThan(0)
                .WithMessage("Assignment ID must be greater than 0.");
        }
    }
}

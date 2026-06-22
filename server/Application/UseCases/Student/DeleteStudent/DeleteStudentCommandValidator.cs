using FluentValidation;
using SmartGrader.Application.UseCases.Lessons.DeleteLesson;

namespace SmartGrader.Application.UseCases.Students.DeleteStudent
{
    public class DeleteStudentCommandValidator : AbstractValidator<DeleteStudentCommand>
    {
        public DeleteStudentCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");
        }
    }
}

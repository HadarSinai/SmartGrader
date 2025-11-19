using FluentValidation;
using SmartGrader.Application.UseCases.Lessons.DeleteLesson;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
    public class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
    {
        public DeleteLessonCommandValidator()
        {
            RuleFor(x => x.Id)
           .GreaterThan(0)
           .WithMessage("Id must be greater than zero.");
        }
    }
}


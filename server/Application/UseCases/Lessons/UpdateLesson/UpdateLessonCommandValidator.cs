using FluentValidation;
using System;

namespace SmartGrader.Application.UseCases.Lessons.UpdateLesson
{
    public class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
    {
        public UpdateLessonCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Lesson ID must be greater than 0");

            RuleFor(x => x.Dto.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Dto.Subject)
                .NotEmpty().WithMessage("Subject is required")
                .MaximumLength(100);

            RuleFor(x => x.Dto.TeacherName)
                .NotEmpty().WithMessage("Teacher name is required")
                .MaximumLength(100);

            RuleFor(x => x.Dto.LessonDate)
                .NotEmpty().WithMessage("Lesson date is required")
                .GreaterThan(DateTime.MinValue)
                .WithMessage("Lesson date is invalid");
        }
    }
}



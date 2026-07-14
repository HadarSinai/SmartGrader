using FluentValidation;
using SmartGrader.Application.Common.HebrewDate;
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

            RuleFor(x => x.Dto.HebrewYear)
                .InclusiveBetween(5000, 6000).WithMessage("שנה עברית לא תקינה");

            RuleFor(x => x.Dto.HebrewMonth)
                .InclusiveBetween(1, 13);

            RuleFor(x => x.Dto.HebrewDay)
                .InclusiveBetween(1, 30);

            RuleFor(x => x.Dto)
                .Must(d => HebrewDateConverter.IsValidHebrewDate(d.HebrewYear, d.HebrewMonth, d.HebrewDay))
                .WithMessage("התאריך העברי אינו קיים");
        }
    }
}



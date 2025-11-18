using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.UpdateStudent
{
    public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
    {
        public UpdateStudentCommandValidator()
        {
            // מזהה התלמיד חייב להיות תקין
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Student ID must be greater than 0");

            // שם מלא
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("FullName is required")
                .MaximumLength(100).WithMessage("FullName cannot exceed 100 characters");

            // שם כיתה
            RuleFor(x => x.ClassName)
                .NotEmpty().WithMessage("ClassName is required")
                .MaximumLength(50).WithMessage("ClassName cannot exceed 50 characters");
        }
    }
}

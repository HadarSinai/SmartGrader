using FluentValidation;

namespace SmartGrader.Application.UseCases.Students.ImportStudents
{
    public class ImportStudentsCommandValidator : AbstractValidator<ImportStudentsCommand>
    {
        public ImportStudentsCommandValidator()
        {
            RuleFor(x => x.FileStream)
                .NotNull().WithMessage("יש להעלות קובץ")
                .Must(s => s.Length > 0).WithMessage("הקובץ ריק");
        }
    }
}

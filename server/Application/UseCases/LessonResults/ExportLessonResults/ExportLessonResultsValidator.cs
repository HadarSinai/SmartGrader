using FluentValidation;

namespace SmartGrader.Application.UseCases.LessonResults.ExportLessonResults;

public class ExportLessonResultsValidator : AbstractValidator<ExportLessonResultsQuery>
{
    public ExportLessonResultsValidator()
    {
        RuleFor(x => x.LessonId).GreaterThan(0).WithMessage("Id must be greater than 0.");
    }
}

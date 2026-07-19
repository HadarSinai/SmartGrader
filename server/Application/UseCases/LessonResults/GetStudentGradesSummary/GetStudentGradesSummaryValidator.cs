using FluentValidation;

namespace SmartGrader.Application.UseCases.LessonResults.GetStudentGradesSummary;

public class GetStudentGradesSummaryValidator : AbstractValidator<GetStudentGradesSummaryQuery>
{
    public GetStudentGradesSummaryValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("Id must be greater than 0.");
    }
}

using FluentValidation;

namespace SmartGrader.Application.UseCases.LessonResults.GetLessonResult;

public class GetLessonResultQueryValidator : AbstractValidator<GetLessonResultQuery>
{
    public GetLessonResultQueryValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("Student Id must be greater than 0.");

        RuleFor(x => x.LessonId)
            .GreaterThan(0).WithMessage("Lesson Id must be greater than 0.");
    }
}

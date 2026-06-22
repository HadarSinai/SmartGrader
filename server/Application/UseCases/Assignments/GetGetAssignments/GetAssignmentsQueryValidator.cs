using FluentValidation;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignments
{
    public class GetAssignmentsQueryValidator : AbstractValidator<GetAssignmentsQuery>
    {
        public GetAssignmentsQueryValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0)
                .WithMessage("LessonId must be greater than 0");
        }
    }
}

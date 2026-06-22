using FluentValidation;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignmentById
{
    public class GetAssignmentByIdQueryValidator : AbstractValidator<GetAssignmentByIdQuery>
    {
        public GetAssignmentByIdQueryValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0);

            RuleFor(x => x.AssignmentId)
                .GreaterThan(0);
        }
    }
}

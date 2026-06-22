using FluentValidation;

namespace SmartGrader.Application.UseCases.Students.GetStudentById
{
    public class GetStudentByIdValidator : AbstractValidator<GetStudentByIdQuery>
    {
        public GetStudentByIdValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");
        }
    }
}

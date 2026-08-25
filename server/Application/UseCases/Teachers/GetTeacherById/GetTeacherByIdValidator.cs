using FluentValidation;

namespace SmartGrader.Application.UseCases.Teachers.GetTeacherById
{
    public class GetTeacherByIdValidator : AbstractValidator<GetTeacherByIdQuery>
    {
        public GetTeacherByIdValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}

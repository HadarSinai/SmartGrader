using FluentValidation;

namespace SmartGrader.Application.UseCases.Classes.GetClassById
{
    public class GetClassByIdQueryValidator : AbstractValidator<GetClassByIdQuery>
    {
        public GetClassByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}

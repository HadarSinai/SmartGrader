using FluentValidation;

namespace SmartGrader.Application.UseCases.Auth.GetMyProfile
{
    public class GetMyProfileQueryValidator : AbstractValidator<GetMyProfileQuery>
    {
        public GetMyProfileQueryValidator()
        {
            RuleFor(x => x.CurrentUserId).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}

using FluentValidation;

namespace SmartGrader.Application.Common.Validation
{
    public static class UsernameRuleExtensions
    {
        public static IRuleBuilderOptions<T, string> Username<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(50).WithMessage("Username must be at most 50 characters long.")
                .Matches(@"^\S+$").WithMessage("Username cannot contain spaces.");
        }
    }
}

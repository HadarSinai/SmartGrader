using FluentValidation;

namespace SmartGrader.Application.Common.Validation
{
    public static class PasswordRuleExtensions
    {
        public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .Must(p => p is null || !System.Text.RegularExpressions.Regex.IsMatch(p, "[\u0590-\u05FF]"))
                    .WithMessage("Password must not contain Hebrew characters.");
        }
    }
}

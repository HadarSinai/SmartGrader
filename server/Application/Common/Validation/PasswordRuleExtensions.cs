using FluentValidation;

namespace SmartGrader.Application.Common.Validation
{
    public static class PasswordRuleExtensions
    {
        /// <summary>
        /// כלל FluentValidation לסיסמה. הכללים עצמם אינם כאן אלא ב-<see cref="PasswordPolicy"/>,
        /// כדי שהייבוא מ-Excel והטפסים יאכפו בדיוק את אותו דבר — ר' ההערה שם.
        /// </summary>
        public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Must(PasswordPolicy.IsValid)
                .WithMessage((_, password) => PasswordPolicy.GetFailureReason(password) ?? "");
        }
    }
}

using FluentAssertions;
using SmartGrader.Application.Common.Validation;
using Xunit;

namespace SmartGrader.UnitTests.Validation
{
    /// <summary>
    /// מקור האמת היחיד לכללי הסיסמה. 🔴 הפער שזה סגר: <c>ImportStudentsHandler</c> מימש את
    /// הכללים מחדש בשורה אחת והשמיט את בדיקת האותיות בעברית. סיסמה עם אותיות עבריות
    /// התקבלה דרך ייבוא Excel ונדחתה בטופס — כלומר תלמידה נכנסה עם סיסמה שהמערכת עצמה
    /// מצהירה שאינה חוקית.
    /// </summary>
    public class PasswordPolicyTests
    {
        private const string Valid = "Sod1234Abc";

        // סיסמה שעומדת בכל הכללים עוברת
        [Fact]
        public void IsValid_AcceptsAPasswordThatMeetsEveryRule()
        {
            PasswordPolicy.IsValid(Valid).Should().BeTrue();
            PasswordPolicy.GetFailureReason(Valid).Should().BeNull();
        }

        // כל אחד מהכללים לבדו פוסל
        [Theory]
        [InlineData("Sod12Ab")]      // קצרה מהמינימום
        [InlineData("sod1234abc")]   // בלי אות גדולה
        [InlineData("SOD1234ABC")]   // בלי אות קטנה
        [InlineData("SodAbcdefg")]   // בלי ספרה
        [InlineData("")]             // ריקה
        [InlineData("   ")]          // רווחים בלבד
        [InlineData(null)]           // חסרה לגמרי
        public void IsValid_RejectsAPasswordThatBreaksARule(string? password)
        {
            PasswordPolicy.IsValid(password).Should().BeFalse();
            PasswordPolicy.GetFailureReason(password).Should().NotBeNullOrWhiteSpace();
        }

        // 🔴 הכלל שאבד פעם: אותיות עבריות נפסלות, גם כשכל שאר הכללים מתקיימים
        [Fact]
        public void IsValid_RejectsHebrewLetters()
        {
            PasswordPolicy.IsValid("Sod1234אבג").Should().BeFalse();
        }

        // הסיסמה התקינה שונה מהפסולה רק באותיות העבריות — כך שהכלל הזה, ולא אחר, הוא שתפס
        [Fact]
        public void IsValid_AcceptsTheSamePasswordWithoutTheHebrewLetters()
        {
            PasswordPolicy.IsValid("Sod1234abc").Should().BeTrue();
        }

        // אורך המינימום צמוד לקבוע ולא למספר קשיח — בדיוק עליו הסיסמה כבר תקינה
        [Fact]
        public void IsValid_AcceptsExactlyTheMinimumLength()
        {
            var atMinimum = "Sod12345";

            atMinimum.Should().HaveLength(PasswordPolicy.MinLength);
            PasswordPolicy.IsValid(atMinimum).Should().BeTrue();
        }

        // ⚠️ סיבה אחת ולא רשימה: המסך והייבוא מציגים שניהם שורה אחת. סיסמה שנכשלת גם
        // באורך וגם בעברית מדווחת על האורך — הכשל הראשון הוא זה שצריך לתקן ממילא.
        [Fact]
        public void GetFailureReason_ReportsTheFirstFailureOnly()
        {
            PasswordPolicy.GetFailureReason("אב1A")
                .Should().Be(PasswordPolicy.GetFailureReason("Sod1A"));
        }
    }
}

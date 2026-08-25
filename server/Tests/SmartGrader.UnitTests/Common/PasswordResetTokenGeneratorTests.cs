using FluentAssertions;
using SmartGrader.Application.Common.Security;
using Xunit;

namespace SmartGrader.UnitTests.Common
{
    /// <summary>
    /// מחולל טוקן איפוס הסיסמה. שומר על ההחלטה המתועדת <b>לא</b> להשתמש ב-
    /// <c>IPasswordHasherService</c>: המלח האקראי שלו מייצר פלט שונה בכל קריאה, ואז אי
    /// אפשר לחפש את הטוקן בטבלה — כלומר אף קישור איפוס לא היה עובד.
    /// </summary>
    public class PasswordResetTokenGeneratorTests
    {
        // כל קריאה מייצרת טוקן אחר — אחרת שתי מורות היו מקבלות את אותו קישור
        [Fact]
        public void Generate_ReturnsDifferentTokenEachCall()
        {
            var first = PasswordResetTokenGenerator.Generate();
            var second = PasswordResetTokenGenerator.Generate();

            first.Should().NotBe(second);
        }

        // ⚠️ הגיבוב יציב: אותו טוקן נותן תמיד אותו גיבוב, ולכן אפשר לחפש לפיו
        [Fact]
        public void Hash_IsStableForSameToken()
        {
            var token = PasswordResetTokenGenerator.Generate();

            PasswordResetTokenGenerator.Hash(token)
                .Should().Be(PasswordResetTokenGenerator.Hash(token));
        }

        // טוקנים שונים נותנים גיבובים שונים
        [Fact]
        public void Hash_DiffersForDifferentTokens()
        {
            var first = PasswordResetTokenGenerator.Hash("token-a");
            var second = PasswordResetTokenGenerator.Hash("token-b");

            first.Should().NotBe(second);
        }

        // הגיבוב אינו הטוקן — מי שקוראת את מסד הנתונים לא יכולה להתחזות
        [Fact]
        public void Hash_DoesNotReturnRawToken()
        {
            var token = PasswordResetTokenGenerator.Generate();

            PasswordResetTokenGenerator.Hash(token).Should().NotBe(token);
        }

        // הטוקן נכנס ל-query string כמו שהוא — בלי תווים שדורשים קידוד
        [Fact]
        public void Generate_IsUrlSafe()
        {
            var token = PasswordResetTokenGenerator.Generate();

            token.Should().NotContainAny("+", "/", "=");
        }

        // גם הגיבוב נשמר בצורה בטוחה ל-URL ולעמודה
        [Fact]
        public void Hash_IsUrlSafe()
        {
            var hash = PasswordResetTokenGenerator.Hash("any-token");

            hash.Should().NotContainAny("+", "/", "=");
        }

        // 256 ביט של אקראיות — לא ניתן לניחוש בתוך שעת התוקף
        [Fact]
        public void Generate_Encodes32Bytes()
        {
            PasswordResetTokenGenerator.Generate().Should().HaveLength(43);
        }
    }
}

using FluentAssertions;
using SmartGrader.Domain.Entities;
using Xunit;

namespace SmartGrader.UnitTests.Domain
{
    /// <summary>
    /// קישור איפוס הסיסמה. שלושת המצבים שבהם טוקן חייב להפסיק לעבוד — פג, נוצל,
    /// או שקישור חדש גבר עליו — הם אותה בדיקה אחת: <see cref="PasswordResetToken.IsUsable"/>.
    /// </summary>
    public class PasswordResetTokenTests
    {
        private static readonly DateTime Now = new(2026, 8, 24, 10, 0, 0, DateTimeKind.Utc);

        private static PasswordResetToken NewToken() =>
            PasswordResetToken.Create(userId: 7, tokenHash: "sha256-hash", utcNow: Now);

        // טוקן טרי שמיש, ותוקפו שעה מרגע היצירה
        [Fact]
        public void NewToken_IsUsableAndExpiresInOneHour()
        {
            var token = NewToken();

            token.IsUsable(Now).Should().BeTrue();
            token.ExpiresAt.Should().Be(Now.Add(PasswordResetToken.Lifetime));
        }

        // רגע לפני הפקיעה שמיש, בדיוק בפקיעה כבר לא
        [Theory]
        [InlineData(59, true)]
        [InlineData(60, false)]
        [InlineData(61, false)]
        public void IsUsable_ExpiresAfterLifetime(int minutesLater, bool expected)
        {
            var token = NewToken();

            token.IsUsable(Now.AddMinutes(minutesLater)).Should().Be(expected);
        }

        // טוקן שנוצל אינו שמיש שוב — קישור חד-פעמי
        [Fact]
        public void IsUsable_IsFalse_AfterUse()
        {
            var token = NewToken();

            token.MarkUsed(Now);

            token.IsUsable(Now).Should().BeFalse();
        }

        // ⚠️ סגירה חוזרת אינה דורסת את החותמת המקורית
        [Fact]
        public void MarkUsed_KeepsOriginalTimestamp()
        {
            var token = NewToken();
            token.MarkUsed(Now);

            token.MarkUsed(Now.AddMinutes(10));

            token.UsedAt.Should().Be(Now);
        }
    }
}

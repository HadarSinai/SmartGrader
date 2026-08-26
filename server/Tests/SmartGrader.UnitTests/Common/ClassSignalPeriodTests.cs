using FluentAssertions;
using SmartGrader.Application.Services.Notifications;
using Xunit;

namespace SmartGrader.UnitTests.Common
{
    /// <summary>
    /// גבול היום של הדיג'סט. ⚠️ הוא נקבע <b>בשעון ישראל ולא ב-UTC</b>: ההגשות נשמרות ב-UTC,
    /// ובישראל (UTC+2/+3) הגשה של 01:00 בלילה נופלת ב-UTC של <i>היום הקודם</i>. חיתוך לפי
    /// UTC היה מפצל שיעור ערב בין שני דיג'סטים.
    /// <para>
    /// הבדיקות כאן נשענות על תכונות מבניות ולא על מספרים קשיחים, כי ההיסט משתנה עם שעון
    /// הקיץ — ובדיקה שמחשבת את ההיסט בעצמה הייתה משכפלת את הקוד שהיא בודקת.
    /// </para>
    /// </summary>
    public class ClassSignalPeriodTests
    {
        private static readonly DateTime Noon = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

        // 🔴 הגבול אינו חצות UTC. אם אזור הזמן של ישראל אינו זמין על מכונת ה-CI, הערך
        // יחזור ל-UTC והבדיקה הזו תאדים — וזה בדיוק מה שצריך לקרות.
        [Fact]
        public void PreviousDay_DoesNotCutOnUtcMidnight()
        {
            var (fromUtc, toUtc) = ClassSignalPeriod.PreviousDay(Noon);

            fromUtc.Hour.Should().NotBe(0);
            toUtc.Hour.Should().NotBe(0);
        }

        // החלון מכסה יום מקומי אחד. הטווח 23–25 שעות ולא 24 בדיוק, כי יום מעבר שעון קצר
        // או ארוך בשעה.
        [Fact]
        public void PreviousDay_CoversOneLocalDay()
        {
            var (fromUtc, toUtc) = ClassSignalPeriod.PreviousDay(Noon);

            (toUtc - fromUtc).Should().BeGreaterThanOrEqualTo(TimeSpan.FromHours(23))
                .And.BeLessThanOrEqualTo(TimeSpan.FromHours(25));
        }

        // ⚠️ חלונות של ימים עוקבים נושקים זה לזה בדיוק. בלי זה יש חור או חפיפה בין שני
        // דיג'סטים — כלומר הגשה שלא דווחה כלל, או שדווחה פעמיים.
        [Fact]
        public void PreviousDay_WindowsOfConsecutiveDaysAreContiguous()
        {
            var yesterday = ClassSignalPeriod.PreviousDay(Noon);
            var today = ClassSignalPeriod.PreviousDay(Noon.AddDays(1));

            today.FromUtc.Should().Be(yesterday.ToUtc);
        }

        // אותו יום מקומי מחזיר את אותו חלון, בלי קשר לשעה שבה העבודה רצה
        [Fact]
        public void PreviousDay_IsTheSameWindow_ThroughoutTheDay()
        {
            var atSix = ClassSignalPeriod.PreviousDay(new DateTime(2026, 3, 10, 6, 0, 0, DateTimeKind.Utc));
            var atEleven = ClassSignalPeriod.PreviousDay(new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc));

            atSix.Should().Be(atEleven);
        }

        // ── שעת ה-Cron ──

        // Hangfire רץ ב-UTC, ולכן השעה המקומית שהוגדרה מומרת. ישראל היא UTC+2 או UTC+3,
        // אז 06:00 מקומי הוא 03:00 או 04:00.
        [Fact]
        public void LocalHourToUtcHour_ShiftsByTheIsraeliOffset()
        {
            ClassSignalPeriod.LocalHourToUtcHour(6).Should().BeOneOf(3, 4);
        }

        // ההמרה עוברת נכון את חצות ואינה יוצאת מהתחום
        [Fact]
        public void LocalHourToUtcHour_WrapsAroundMidnight()
        {
            ClassSignalPeriod.LocalHourToUtcHour(0).Should().BeOneOf(21, 22);
        }

        // ערך מחוץ לתחום נקטם ואינו מייצר שעת cron בלתי חוקית
        [Theory]
        [InlineData(99, 23)]
        [InlineData(-5, 0)]
        public void LocalHourToUtcHour_ClampsOutOfRangeInput(int configured, int equivalentHour)
        {
            ClassSignalPeriod.LocalHourToUtcHour(configured)
                .Should().Be(ClassSignalPeriod.LocalHourToUtcHour(equivalentHour));
        }
    }
}

using FluentAssertions;
using SmartGrader.Application.Common.HebrewDate;
using Xunit;

namespace SmartGrader.UnitTests.Common
{
    /// <summary>
    /// המרת תאריכים עבריים. מספור החודשים ב-.NET הוא תשרי=1, ובשנה מעוברת יש 13 חודשים
    /// (אדר א׳=6, אדר ב׳=7) — כלומר <b>אותו מספר חודש מציין חודש אחר בשנה מעוברת</b>.
    /// זה סוג הבאג שצץ פעם בשלוש שנים ושובר את כל התאריכים בבת אחת.
    /// <para>
    /// שנים בשימוש: 5784 ו-5787 מעוברות, 5785 ו-5786 פשוטות — לפי (7y+1) mod 19 &lt; 7.
    /// </para>
    /// </summary>
    public class HebrewDateConverterTests
    {
        // ── הלוך ושוב: המרה וחזרה מחזירות בדיוק את אותם רכיבים ──

        // תאריך שהוזן נשמר ומוצג כאותו תאריך — בשנה פשוטה ובמעוברת כאחת
        [Theory]
        [InlineData(5786, 1, 1)]    // א' תשרי, שנה פשוטה
        [InlineData(5786, 4, 29)]   // כ"ט טבת — סוף חודש בן 29 יום
        [InlineData(5786, 12, 29)]  // כ"ט אלול — היום האחרון של שנה פשוטה
        [InlineData(5784, 6, 30)]   // ל' אדר א׳ — קיים רק בשנה מעוברת
        [InlineData(5784, 7, 29)]   // כ"ט אדר ב׳
        [InlineData(5784, 13, 29)]  // כ"ט אלול — החודש ה-13 של שנה מעוברת
        public void ToGregorian_RoundTrips_ThroughGetHebrewParts(int year, int month, int day)
        {
            var gregorian = HebrewDateConverter.ToGregorian(year, month, day);

            HebrewDateConverter.GetHebrewParts(gregorian).Should().Be((year, month, day));
        }

        // התאריך נשמר בחצות — לשדה תאריך עברי אין שעה
        [Fact]
        public void ToGregorian_StoresMidnight()
        {
            var gregorian = HebrewDateConverter.ToGregorian(5786, 9, 14);

            gregorian.TimeOfDay.Should().Be(TimeSpan.Zero);
        }

        // ── אדר א׳ מול אדר ב׳: אותו מספר, חודש אחר ──

        // בשנה מעוברת חודש 6 (אדר א׳) וחודש 7 (אדר ב׳) הם תאריכים שונים, וא׳ קודם לב׳
        [Fact]
        public void LeapYear_SeparatesAdarIFromAdarII()
        {
            var adarI = HebrewDateConverter.ToGregorian(5784, 6, 1);
            var adarII = HebrewDateConverter.ToGregorian(5784, 7, 1);

            adarI.Should().BeBefore(adarII);
        }

        // ── תקינות התאריך ──

        // חודש 13 קיים רק בשנה מעוברת — בשנה פשוטה הוא לא קיים כלל
        [Theory]
        [InlineData(5784, true)]   // מעוברת
        [InlineData(5787, true)]   // מעוברת
        [InlineData(5785, false)]  // פשוטה
        [InlineData(5786, false)]  // פשוטה
        public void IsValidHebrewDate_AllowsThirteenthMonth_OnlyInLeapYear(int year, bool expected)
        {
            HebrewDateConverter.IsValidHebrewDate(year, 13, 1).Should().Be(expected);
        }

        // טבת הוא תמיד בן 29 יום — ל' טבת אינו קיים
        [Fact]
        public void IsValidHebrewDate_RejectsDayThirtyInTwentyNineDayMonth()
        {
            HebrewDateConverter.IsValidHebrewDate(5786, 4, 30).Should().BeFalse();
        }

        // גבולות הטווח הנתמך של HebrewCalendar — מחוץ להם ToGregorian זורק
        [Theory]
        [InlineData(5342, false)]
        [InlineData(5343, true)]
        [InlineData(5999, true)]
        [InlineData(6000, false)]
        public void IsValidHebrewDate_EnforcesSupportedYearRange(int year, bool expected)
        {
            HebrewDateConverter.IsValidHebrewDate(year, 1, 1).Should().Be(expected);
        }

        // חודש או יום מחוץ לתחום נדחים
        [Theory]
        [InlineData(5786, 0, 1)]
        [InlineData(5786, 14, 1)]
        [InlineData(5786, 1, 0)]
        [InlineData(5786, 1, 31)]
        public void IsValidHebrewDate_RejectsOutOfRangeMonthOrDay(int year, int month, int day)
        {
            HebrewDateConverter.IsValidHebrewDate(year, month, day).Should().BeFalse();
        }

        // ── תצוגת גימטריה ──

        // התאריך מוצג בגימטריה עברית, לא בספרות
        [Fact]
        public void ToHebrewString_RendersGematria()
        {
            var text = HebrewDateConverter.ToHebrewString(HebrewDateConverter.ToGregorian(5786, 9, 14));

            text.Should().NotBeNullOrWhiteSpace();
            text.Should().NotContain("5786");
            text.Should().MatchRegex("[֐-׿]");
        }

        // שנה בלבד בגימטריה — בלי יום וחודש
        [Fact]
        public void ToHebrewYearString_RendersYearOnly()
        {
            var text = HebrewDateConverter.ToHebrewYearString(5786);

            text.Should().NotBeNullOrWhiteSpace();
            text.Should().NotContain("5786");
        }

        // השנה העברית הנוכחית נופלת בטווח הנתמך — שמירה מפני חישוב שיצא מהתחום
        [Fact]
        public void GetCurrentHebrewYear_IsWithinSupportedRange()
        {
            HebrewDateConverter.GetCurrentHebrewYear().Should().BeInRange(5343, 5999);
        }
    }
}

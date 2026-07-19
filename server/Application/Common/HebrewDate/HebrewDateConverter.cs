using System.Globalization;

namespace SmartGrader.Application.Common.HebrewDate
{
    /// <summary>
    /// Pure static utility for Hebrew ↔ Gregorian date conversion via System.Globalization.HebrewCalendar.
    /// Month numbering follows .NET: Tishrei = 1; leap year has 13 months (Adar I = 6, Adar II = 7).
    /// Not registered in DI — callable from AutoMapper MapFrom lambdas and validators.
    /// </summary>
    public static class HebrewDateConverter
    {
        private static readonly HebrewCalendar Calendar = new();

        public static DateTime ToGregorian(int hebrewYear, int hebrewMonth, int hebrewDay)
            => Calendar.ToDateTime(hebrewYear, hebrewMonth, hebrewDay, 0, 0, 0, 0); // midnight

        public static (int Year, int Month, int Day) GetHebrewParts(DateTime date)
            => (Calendar.GetYear(date), Calendar.GetMonth(date), Calendar.GetDayOfMonth(date));

        public static string ToHebrewString(DateTime date)
        {
            var ci = new CultureInfo("he-IL");
            ci.DateTimeFormat.Calendar = new HebrewCalendar();
            return date.ToString("dd MMMM yyyy", ci); // → י"ד תמוז תשפ"ו
        }

        // שנה עברית נוכחית (למשל 5786) — לפי התאריך של היום
        public static int GetCurrentHebrewYear()
            => Calendar.GetYear(DateTime.Today);

        // פורמט גימטריה של שנה בלבד: 5786 → "תשפ"ו"
        public static string ToHebrewYearString(int hebrewYear)
        {
            var ci = new CultureInfo("he-IL");
            ci.DateTimeFormat.Calendar = new HebrewCalendar();
            return Calendar.ToDateTime(hebrewYear, 1, 1, 0, 0, 0, 0).ToString("yyyy", ci);
        }

        public static bool IsValidHebrewDate(int year, int month, int day)
        {
            if (year < 5343 || year > 5999) return false; // HebrewCalendar supported range
            if (month < 1 || month > Calendar.GetMonthsInYear(year)) return false;
            return day >= 1 && day <= Calendar.GetDaysInMonth(year, month);
        }
    }
}

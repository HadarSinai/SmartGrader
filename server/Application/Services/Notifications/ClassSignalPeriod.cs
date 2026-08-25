namespace SmartGrader.Application.Services.Notifications
{
    /// <summary>
    /// חלון הזמן שעליו מחושבים הסיגנלים. <b>מקור אחד לפעמון ולדיג'סט</b> — הפעמון מציג
    /// בדיוק את מה שהמייל שולח, ולכן שניהם חייבים לשאול על אותו יום בדיוק.
    /// </summary>
    public static class ClassSignalPeriod
    {
        /// <summary>
        /// ⚠️ גבול היום נקבע בשעון ישראל ולא ב-UTC. ההגשות נשמרות ב-UTC, ובחורף ישראל
        /// היא UTC+2 — כלומר הגשה של 23:00 ביום שני נופלת ב-21:00 UTC של אותו יום, אבל
        /// הגשה של 01:00 בלילה נופלת ב-23:00 UTC של <i>היום הקודם</i>. חיתוך לפי UTC היה
        /// מפצל שיעור ערב בין שני דיג'סטים.
        /// </summary>
        private static readonly TimeZoneInfo LocalZone = ResolveLocalZone();

        /// <summary>
        /// החלון של אתמול, כטווח UTC חצי־פתוח <c>[From, To)</c>.
        /// </summary>
        public static (DateTime FromUtc, DateTime ToUtc) PreviousDay(DateTime nowUtc)
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, LocalZone);
            var yesterday = localNow.Date.AddDays(-1);

            return (ToUtc(yesterday), ToUtc(yesterday.AddDays(1)));
        }

        /// <summary>
        /// ממירה שעה בשעון ישראל לשעת UTC, עבור ביטוי ה-Cron של Hangfire (שהוא UTC).
        /// <para>
        /// ⚠️ ההיסט מחושב לפי <b>היום</b>, ו-Hangfire שומר את הביטוי כפי שהוא. כלומר אחרי
        /// מעבר שעון קיץ העבודה תרוץ שעה מוקדם או מאוחר מהמתוכנן, עד ההפעלה הבאה של השרת.
        /// לדיג'סט יומי זה מקובל; לעבודה רגישה לשעה זה לא היה מספיק.
        /// </para>
        /// </summary>
        public static int LocalHourToUtcHour(int localHour)
        {
            var hour = Math.Clamp(localHour, 0, 23);
            var offset = LocalZone.GetUtcOffset(DateTime.UtcNow).Hours;

            return ((hour - offset) % 24 + 24) % 24;
        }

        private static DateTime ToUtc(DateTime localDate) =>
            TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified),
                LocalZone);

        /// <summary>
        /// מזהה IANA קודם (Linux/Docker, וגם Windows מ-.NET 6 ואילך דרך ICU), ואחריו המזהה
        /// הישן של Windows. נפילה ל-UTC היא מוצא אחרון: היא מזיזה את גבול היום בשעתיים,
        /// אבל היא לא מפילה את העבודה.
        /// </summary>
        private static TimeZoneInfo ResolveLocalZone()
        {
            foreach (var id in new[] { "Asia/Jerusalem", "Israel Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            return TimeZoneInfo.Utc;
        }
    }
}

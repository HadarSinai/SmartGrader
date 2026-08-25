using System.Text.RegularExpressions;

namespace SmartGrader.Application.Common.Validation
{
    /// <summary>
    /// מקור האמת היחיד לכללי הסיסמה במערכת.
    /// <para>
    /// 🔴 הפער שזה סוגר: <c>ImportStudentsHandler</c> מימש את הכללים מחדש בשורה אחת
    /// (אורך, אות גדולה, אות קטנה, ספרה) והשמיט את בדיקת האותיות בעברית שכן נאכפה בכל
    /// מסלול אחר. סיסמה עם אותיות עבריות התקבלה דרך ייבוא Excel ונדחתה בטופס — כלומר
    /// תלמידה נכנסה עם סיסמה שהמערכת מצהירה שאינה חוקית.
    /// </para>
    /// <para>
    /// ⚠️ אין להעתיק מכאן כללים לשום מקום. כל מסלול שבודק סיסמה קורא לפונקציה הזו —
    /// זה כל מה שמונע את הסטייה הבאה.
    /// </para>
    /// </summary>
    public static class PasswordPolicy
    {
        public const int MinLength = 8;

        /// <summary>טווח האותיות העבריות ב-Unicode.</summary>
        private const string HebrewLetters = "[\\u0590-\\u05FF]";

        /// <summary>
        /// מחזירה את סיבת הפסילה בעברית, או <c>null</c> כשהסיסמה תקינה.
        /// <para>
        /// סיבה אחת ולא רשימה: המסך והייבוא מציגים שניהם שורה אחת, והכשלון הראשון הוא
        /// זה שצריך לתקן ממילא.
        /// </para>
        /// </summary>
        public static string? GetFailureReason(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "יש להזין סיסמה";

            if (password.Length < MinLength)
                return $"סיסמה חייבת להכיל לפחות {MinLength} תווים";

            if (!Regex.IsMatch(password, "[A-Z]"))
                return "סיסמה חייבת להכיל אות גדולה באנגלית";

            if (!Regex.IsMatch(password, "[a-z]"))
                return "סיסמה חייבת להכיל אות קטנה באנגלית";

            if (!Regex.IsMatch(password, "[0-9]"))
                return "סיסמה חייבת להכיל ספרה";

            if (Regex.IsMatch(password, HebrewLetters))
                return "סיסמה לא יכולה להכיל אותיות בעברית";

            return null;
        }

        public static bool IsValid(string? password) => GetFailureReason(password) is null;
    }
}

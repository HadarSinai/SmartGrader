using System.Security.Cryptography;

namespace SmartGrader.Application.Common.Security
{
    /// <summary>
    /// מייצרת את הטוקן החד-פעמי לאיפוס סיסמה, ומגבבת אותו לצורך אחסון.
    /// מקור אמת אחד לשני ה-handlers: <c>ForgotPassword</c> מגבב כדי לשמור,
    /// ו-<c>ResetPassword</c> מגבב כדי לחפש. אם השניים היו מממשים גיבוב בנפרד,
    /// כל שינוי באחד מהם היה הופך בשקט כל קישור קיים ללא-תקף.
    /// </summary>
    public static class PasswordResetTokenGenerator
    {
        /// <summary>256 ביט של אקראיות קריפטוגרפית — לא ניתן לניחוש בתוך שעת התוקף.</summary>
        private const int TokenBytes = 32;

        /// <summary>
        /// טוקן גולמי, בטוח לשילוב ב-query string.
        /// ⚠️ הערך המוחזר קיים רק בקישור שנשלח במייל. הוא לא נשמר, ולא נכתב ללוג, ולא
        /// חוזר בגוף התשובה — מי שמחזיקה אותו יכולה לאפס את הסיסמה.
        /// </summary>
        public static string Generate()
        {
            return Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        }

        /// <summary>
        /// גיבוב SHA-256 של הטוקן.
        /// <para>
        /// ⚠️ בכוונה <b>לא</b> <c>IPasswordHasherService</c>, למרות שזו אותה מטרה של "לא לשמור
        /// את הסוד עצמו". הגיבוב שם (PBKDF2 של ASP.NET Identity) משתמש במלח אקראי לכל קריאה,
        /// ולכן אותו קלט מייצר פלט שונה בכל פעם — אי אפשר לחפש לפיו בטבלה, וזו בדיוק הפעולה
        /// היחידה שנדרשת כאן. ה-KDF האיטי שם קיים כדי להגן על סיסמאות שבני אדם בוחרים,
        /// ובעלות ניחוש נמוכה. כאן הקלט הוא 256 ביט אקראיים ואין מה להאט: אין מרחב לחפש בו.
        /// </para>
        /// </summary>
        public static string Hash(string rawToken)
        {
            var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
            return Base64UrlEncode(bytes);
        }

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
    }
}

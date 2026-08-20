using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Authorization
{
    /// <summary>
    /// האם הגשה נעולה סופית — כלומר אין דרך לפתוח אותה מחדש, גם לא באישור מורה.
    /// <para>
    /// זה נפרד לגמרי מ-<see cref="Submission.CanResubmit"/>, שעונה על שאלה אחרת: <i>האם
    /// מצב ההגשה והציון מאפשרים ניסיון נוסף</i>. נעילה גוברת על שניהם.
    /// </para>
    /// </summary>
    public static class SubmissionLock
    {
        public const string Message =
            "לא ניתן להגיש שוב — השיעור כבר סוכם או שהכיתה נמצאת בארכיון.";

        /// <summary>
        /// שני תנאים, וכל אחד מהם לבדו נועל:
        /// <list type="number">
        /// <item><c>LessonResult.IsComplete</c> לתלמידה הזו בשיעור הזה. ⚠️ הציון הסופי הוא
        /// לפי <b>(תלמידה, שיעור)</b> ולא לפי הכיתה — שיעור "מסתיים" לכל תלמידה בנפרד.</item>
        /// <item>הכיתה של התלמידה בארכיון (שנת לימודים שהתגלגלה).</item>
        /// </list>
        /// <para>
        /// ℹ️ התוכנית מנתה תנאי שלישי — "הוגשה לפני שהמנוע עלה" — שנועד למנוע פתיחה
        /// רטרואקטיבית של כל היסטוריית ההגשות ביום הפריסה. הוא <b>לא מומש</b>, לפי החלטה
        /// שה-DB הוא נתוני פיתוח ונמחק. <b>אם המערכת תעלה על DB עם היסטוריה אמיתית, יש
        /// להחזיר אותו לכאן לפני הפריסה</b> — בלעדיו כל הגשה ישנה מתחת לסף נפתחת מיד.
        /// </para>
        /// </summary>
        public static async Task<bool> IsLockedAsync(
            ILessonResultRepository lessonResults,
            Submission submission,
            CancellationToken ct)
        {
            if (submission.Student?.Class?.IsArchived == true)
                return true;

            var lessonId = submission.Assignment?.LessonId;
            if (lessonId is null)
                return false;

            var result = await lessonResults.GetAsync(submission.StudentId, lessonId.Value, ct);
            return result?.IsComplete == true;
        }
    }
}

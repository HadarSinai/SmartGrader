using SmartGrader.Application.Dtos.Submissions;
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
        /// רטרואקטיבית של כל היסטוריית ההגשות ביום הפריסה. הוא <b>לא מומש</b>, ולפי החלטה
        /// מ-30/08/2026 גם לא יידרש: המסד נמחק ומתחיל ריק, ולכן אין הגשה שקדמה למנוע.
        /// <b>שני התנאים כאן נכונים רק כל עוד ההנחה הזו מתקיימת</b> — שחזור מסד ישן או ייבוא
        /// היסטוריית הגשות מחייבים להחזיר את התנאי השלישי לפני אותה פריסה. ר' <c>B-8</c>
        /// ב-docs/business-rules.md.
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

        /// <summary>
        /// מחיל את הנעילה על DTO שכבר מופה — מכבה <c>CanResubmit</c> וממלא <c>LockReason</c>.
        /// </summary>
        /// <remarks>
        /// ⚠️ קיים כי הנעילה אינה ניתנת לחישוב ב-<c>SubmissionProfile</c>: היא דורשת שאילתת
        /// <c>LessonResult</c>, ו-AutoMapper אינו אסינכרוני. בלי הקריאה הזו ה-DTO מחזיר
        /// <c>CanResubmit = true</c> לשיעור שכבר סוכם, מסך התלמידה מציג "תיקון והגשה מחדש",
        /// והלחיצה נופלת ב-<c>MarkPendingAi</c> על כלל שהמסך מעולם לא הזכיר.
        /// <para>
        /// אותה סיבה בדיוק שבגללה <c>TestVisibility</c> רץ אחרי המיפוי, ובאותו מקום בדיוק —
        /// בסוף ה-handler, לפני שה-DTO עוזב אותו.
        /// </para>
        /// </remarks>
        public static async Task<SubmissionResponseDto> ApplyAsync(
            ILessonResultRepository lessonResults,
            SubmissionResponseDto dto,
            Submission submission,
            CancellationToken ct)
        {
            if (!await IsLockedAsync(lessonResults, submission, ct))
                return dto;

            dto.CanResubmit = false;
            dto.LockReason = Message;
            return dto;
        }

        /// <summary>
        /// הגרסה לרשימה. שאילתה אחת לכל התוצאות של התלמידה במקום אחת לכל הגשה.
        /// </summary>
        /// <remarks>
        /// ⚠️ מניח שכל ההגשות שייכות לאותה תלמידה — זה המצב בכל הקוראים היום
        /// (<c>GetByStudentIdAsync</c>). רשימה מעורבת תקבל נעילה של התלמידה הראשונה.
        /// </remarks>
        public static async Task<IReadOnlyList<SubmissionResponseDto>> ApplyAsync(
            ILessonResultRepository lessonResults,
            IReadOnlyList<SubmissionResponseDto> dtos,
            IReadOnlyList<Submission> submissions,
            CancellationToken ct)
        {
            var openForResubmit = dtos.Where(d => d.CanResubmit).ToList();
            if (openForResubmit.Count == 0)
                return dtos;

            var studentId = submissions[0].StudentId;

            // כיתה בארכיון נועלת את כל ההגשות של התלמידה בבת אחת — אין צורך לבדוק שיעורים
            if (submissions[0].Student?.Class?.IsArchived == true)
            {
                foreach (var dto in openForResubmit)
                {
                    dto.CanResubmit = false;
                    dto.LockReason = Message;
                }

                return dtos;
            }

            var completedLessonIds = (await lessonResults.GetByStudentIdAsync(studentId, ct))
                .Where(r => r.IsComplete)
                .Select(r => r.LessonId)
                .ToHashSet();

            if (completedLessonIds.Count == 0)
                return dtos;

            foreach (var dto in openForResubmit.Where(d => completedLessonIds.Contains(d.LessonId)))
            {
                dto.CanResubmit = false;
                dto.LockReason = Message;
            }

            return dtos;
        }
    }
}

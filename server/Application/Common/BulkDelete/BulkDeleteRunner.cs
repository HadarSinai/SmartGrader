using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Common;

namespace SmartGrader.Application.Common.BulkDelete
{
    /// <summary>
    /// מריץ מחיקה בודדת על כל מזהה שנבחר, ואוסף לכל אחד תוצאה משלו.
    /// <para>
    /// 🔴 <b>הקריאה הפנימית היא המחיקה הבודדת עצמה, ולא עותק של הכללים שלה.</b> לכל משאב
    /// כאן יש שומר משלו — תרגיל עם הגשות, שיעור עם ציונים סופיים, תלמידה עם עבודה, הגשה
    /// שכבר נבדקה — ומחיקה מרובה שהייתה מנסחת אותם מחדש הייתה השני מבין שני מקורות אמת.
    /// העותק הוא זה שמתיר בטעות את מה שהמקור חוסם, וכאן המחיר הוא עבודה של תלמידה.
    /// </para>
    /// <para>
    /// ⚠️ <b>הצלחה חלקית אינה נבלעת.</b> שורה שסורבה אינה מבטלת את מה שכבר נמחק, ואינה
    /// עוצרת את מה שאחריה. זה מסתמך על כך שכל מחיקה בודדת מסיימת את הבדיקות שלה <i>לפני</i>
    /// שהיא נוגעת בישות: אחרת סירוב היה משאיר שינוי תלוי, וה-SaveChanges של השורה הבאה היה
    /// כותב אותו.
    /// </para>
    /// <para>
    /// ⚠️ נתפסים שני סוגי חריגה בלבד — סירוב עסקי ומשאב שאינו קיים. תקלת מסד נשארת חריגה
    /// ומגיעה כ-500, כי היא אינה "השורה הזו לא נמחקה" אלא "המערכת אינה במצב שאפשר לסמוך עליו".
    /// </para>
    /// </summary>
    public static class BulkDeleteRunner
    {
        /// <summary>
        /// כמה מזהים מותר בבקשה אחת. מספר שרירותי במכוון, וקיים כדי שבקשה אחת לא תוכל
        /// לגרור אלפי מחיקות — לא כדי להגביל את המורה, שבוחרת שורות במסך מעומד.
        /// </summary>
        public const int MaxIdsPerRequest = 100;

        public const string TooManyIdsMessage =
            "אפשר למחוק עד 100 שורות בבקשה אחת.";

        public const string NotFoundMessage =
            "השורה לא נמצאה — ייתכן שכבר נמחקה.";

        public static async Task<BulkDeleteResultDto> RunAsync(
            IEnumerable<int> ids,
            Func<int, Task> deleteOne,
            CancellationToken ct)
        {
            var result = new BulkDeleteResultDto();

            // ⚠️ Distinct: אותו מזהה פעמיים היה נמחק פעם אחת ומדווח כלא-נמצא בפעם השנייה,
            // כלומר "1 נמחקה, 1 נכשלה" על שורה אחת שנמחקה בהצלחה.
            foreach (var id in ids.Distinct())
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await deleteOne(id);
                    result.DeletedIds.Add(id);
                }
                catch (BusinessRuleException ex)
                {
                    // ההודעה כלשונה מהמחיקה הבודדת: היא כבר מנוסחת למורה ומונה מה חוסם.
                    result.Failures.Add(new BulkDeleteFailureDto { Id = id, Message = ex.Message });
                }
                catch (NotFoundException)
                {
                    // ⚠️ לא נחשפת ההודעה הפנימית: היא נושאת שם טיפוס ומזהה, ומורה שבחרה
                    // שורה שמורה אחרת מחקה בינתיים אינה צריכה לקרוא שם מחלקה.
                    result.Failures.Add(new BulkDeleteFailureDto { Id = id, Message = NotFoundMessage });
                }
            }

            return result;
        }
    }
}

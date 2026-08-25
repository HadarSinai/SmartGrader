using MediatR;
using SmartGrader.Application.Dtos.Notifications;

namespace SmartGrader.Application.UseCases.Notifications.GetClassSignals
{
    /// <summary>
    /// הסיגנלים על הכיתה ועל התרגילים בחלון תאריכים.
    /// <para>
    /// ⚠️ <paramref name="TeacherId"/> הוא סינון הבעלות, בדיוק כמו בכל שאר הקריאות:
    /// מורה רואה רק שיעורים שלה, <c>null</c> = מנהלת ורואה הכול. אין ברירת מחדל בכוונה —
    /// השמטה היא שגיאת קומפילציה ולא דליפה שקטה של הכיתה של מורה אחרת.
    /// </para>
    /// <para>
    /// ⚠️ אין כאן פרמטר "תלמידה". הסיגנלים אינם נמסרים לתלמידה בשום מסלול — הם חושפים כמה
    /// מהכיתה נכשלו ובמה. הפעמון של התלמידה נשאר על <c>GetRecentGradedSubmissionsQuery</c>.
    /// </para>
    /// </summary>
    public record GetClassSignalsQuery(int? TeacherId, DateTime FromUtc, DateTime ToUtc)
        : IRequest<IReadOnlyList<ClassSignalDto>>;
}

using MediatR;

namespace SmartGrader.Application.UseCases.Notifications.SendTeacherDigest
{
    /// <summary>
    /// שולחת את הדיג'סט היומי לכל המורות שיש להן כתובת מייל וסיגנלים בחלון.
    /// <para>
    /// ⚠️ החלון מגיע כפרמטר ולא מחושב בפנים, כדי שאפשר יהיה להריץ את העבודה על יום מסוים
    /// (בדיקה, או השלמה אחרי נפילה) בלי לגעת בשעון המערכת.
    /// </para>
    /// <para>
    /// ⚠️ אין כאן מצב "כבר נשלח". החלון הוא טווח תאריכים קבוע, ולכן הרצה חוזרת על אותו יום
    /// מייצרת בדיוק את אותו מייל — אידמפוטנטי בתוכן, לא בשליחה. טבלת "נשלח" הייתה רושמת
    /// מה שהתאריך כבר קובע. ר' plan-teacherNotifications.
    /// </para>
    /// </summary>
    /// <returns>כמה מיילים נשלחו בפועל.</returns>
    public record SendTeacherDigestCommand(DateTime FromUtc, DateTime ToUtc) : IRequest<int>;
}

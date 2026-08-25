using MediatR;
using SmartGrader.Application.Dtos.Auth;

namespace SmartGrader.Application.UseCases.Auth.ChangeMyPassword
{
    /// <summary>
    /// שינוי הסיסמה של המשתמשת המחוברת עצמה, פתוח לכל התפקידים — גם לתלמידה, שזו הפעולה
    /// היחידה שיש לה באזור האישי.
    /// </summary>
    /// <remarks>
    /// אין תשובה: הפעולה אינה מנפיקה טוקן חדש ואינה מנתקת את ה-session. הסיסמה אינה
    /// claim בתוך הטוקן, ולכן הטוקן הקיים נשאר נכון לחלוטין אחרי ההחלפה — בשונה משינוי
    /// שם, ר' <c>UpdateMyProfileCommand</c>.
    /// </remarks>
    public record ChangeMyPasswordCommand(int CurrentUserId, ChangeMyPasswordRequestDto Dto) : IRequest;
}

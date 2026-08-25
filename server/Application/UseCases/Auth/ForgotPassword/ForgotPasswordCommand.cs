using MediatR;
using SmartGrader.Application.Dtos.Auth;

namespace SmartGrader.Application.UseCases.Auth.ForgotPassword
{
    /// <summary>
    /// ⚠️ אין ערך החזרה במכוון. "נמצאה משתמשת" / "נשלח מייל" הוא בדיוק המידע שהופך את
    /// הנקודה למונה חשבונות רשומים, ולכן הוא לא עוזב את ה-handler.
    /// </summary>
    public record ForgotPasswordCommand(ForgotPasswordRequestDto Dto) : IRequest;
}

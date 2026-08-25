using MediatR;
using SmartGrader.Application.Dtos.Auth;

namespace SmartGrader.Application.UseCases.Auth.GetMyProfile
{
    /// <summary>
    /// פרטי החשבון של המשתמשת המחוברת, לטעינת הטופס באזור האישי.
    /// </summary>
    /// <remarks>
    /// <paramref name="CurrentUserId"/> מגיע מה-claims ב-controller ולא מהנתיב — בלי זה
    /// זו נקודה שמדפדפת בפרטי החשבון של כל משתמשת במערכת לפי מזהה רץ.
    /// </remarks>
    public record GetMyProfileQuery(int CurrentUserId) : IRequest<MyProfileResponseDto>;
}

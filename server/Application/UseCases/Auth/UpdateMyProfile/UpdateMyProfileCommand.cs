using MediatR;
using SmartGrader.Application.Dtos.Auth;

namespace SmartGrader.Application.UseCases.Auth.UpdateMyProfile
{
    /// <summary>
    /// שינוי השם המלא והמייל של המשתמשת המחוברת עצמה.
    /// </summary>
    /// <remarks>
    /// <paramref name="CurrentUserId"/> מגיע מה-claims ב-controller ולא מגוף הבקשה — ר'
    /// ההערה ב-<see cref="UpdateMyProfileRequestDto"/>.
    /// <para>
    /// מחזירה <see cref="AuthResponseDto"/> ולא DTO של פרופיל: השם המלא יושב גם כ-claim
    /// בתוך ה-JWT, וטוקן הוא מחרוזת חתומה שאי אפשר לערוך במקום. בלי טוקן מרוענן, השם הישן
    /// היה ממשיך לחזור מכל מה שקורא את ה-claim (כיום <c>GET /api/auth/me</c>) עד הכניסה הבאה.
    /// </para>
    /// </remarks>
    public record UpdateMyProfileCommand(int CurrentUserId, UpdateMyProfileRequestDto Dto)
        : IRequest<AuthResponseDto>;
}

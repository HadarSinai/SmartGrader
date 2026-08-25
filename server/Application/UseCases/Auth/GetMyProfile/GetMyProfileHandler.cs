using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Auth.GetMyProfile
{
    public class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, MyProfileResponseDto>
    {
        private readonly IUserRepository _userRepository;

        public GetMyProfileHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<MyProfileResponseDto> Handle(
            GetMyProfileQuery request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.CurrentUserId, cancellationToken);

            // המשתמשת נמחקה בזמן שה-session שלה עדיין פתוח.
            if (user is null)
                throw new NotFoundException("User", request.CurrentUserId);

            // ⚠️ בנייה ידנית ולא _mapper.Map, בעקבות שאר ה-handlers בתיקיית Auth
            // (LoginHandler, ResetPasswordHandler): אף אחד מהם אינו מזריק IMapper. הרווח כאן
            // אינו נוחות אלא שליטה — רשימת השדות שיוצאים החוצה כתובה במפורש בשורה אחת,
            // ואי אפשר להוסיף בטעות שדה חדש ל-User ולגלות שהוא זולג החוצה מעצמו.
            return new MyProfileResponseDto(
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.Role.ToString());
        }
    }
}

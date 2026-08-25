using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Auth.UpdateMyProfile
{
    public class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileCommand, AuthResponseDto>
    {
        /// <summary>
        /// ⚠️ תלמידה אינה משנה את שמה בעצמה. <c>User.FullName</c> ו-<c>Student.FullName</c> הם
        /// שני שדות נפרדים, ושינוי אחד מהם היה מפצל בין מה שהיא רואה למה שהמורה שלה רואה.
        /// מייל אין לה כלל. השם שלה מתוחזק בידי המורה במסך התלמידות.
        /// </summary>
        private const string StudentCannotEditProfileMessage =
            "אין באפשרותך לשנות את השם או המייל שלך. יש לפנות למורה שלך.";

        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMyProfileHandler(
            IUserRepository userRepository,
            IJwtTokenGenerator tokenGenerator,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _tokenGenerator = tokenGenerator;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResponseDto> Handle(
            UpdateMyProfileCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.CurrentUserId, cancellationToken);

            // המשתמשת נמחקה בזמן שה-session שלה עדיין פתוח.
            if (user is null)
                throw new NotFoundException("User", request.CurrentUserId);

            // הבקרה האמיתית היא [Authorize(Roles = "Teacher,Admin")] על הנקודה, שמחזירה 403
            // לפני שמגיעים לכאן. הבדיקה הזו היא שכבה שנייה, למקרה שהנקודה תיפתח בעתיד
            // לתפקידים נוספים בלי לשים לב מה זה עושה לתלמידה.
            if (user.Role == UserRole.Student)
                throw new BusinessRuleException(StudentCannotEditProfileMessage);

            // excludingUserId — בלי זה המורה מתנגשת עם המייל של עצמה ומקבלת 409 על שינוי
            // השם בלבד. אותה בדיקה בדיוק כמו ב-UpdateTeacherHandler.
            if (await _userRepository.ExistsByEmailAsync(request.Dto.Email, user.Id, cancellationToken))
                throw new UniqueConstraintException("A user with this email already exists.");

            user.SetFullName(request.Dto.FullName);
            user.SetEmail(request.Dto.Email);

            // GetByIdAsync מחזירה ישות מנותקת (AsNoTracking), ולכן בלי UpdateAsync
            // השינוי פשוט לא נשמר.
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ⚠️ הטוקן מונפק **אחרי** השמירה, לא לפניה: טוקן שנחתם על שם חדש שלא נשמר היה
            // מציג במסך שם שאינו קיים במסד.
            //
            // studentId הוא null בוודאות — הגענו לכאן רק כמורה או כמנהלת.
            var token = _tokenGenerator.GenerateToken(user, studentId: null);

            return new AuthResponseDto(token, user.FullName, user.Role.ToString(), null);
        }
    }
}

using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Auth.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        /// <summary>
        /// ההודעה היחידה לכל מסלולי הכישלון: שם משתמש שאינו קיים, סיסמה שגויה, וחשבון נעול.
        /// <para>
        /// ⚠️ אין לפצל אותה. "החשבון נעול" מאשר לקורא שהחשבון קיים — בדיוק המידע שההודעה
        /// הגנרית נועדה לא למסור. ההסבר על הנעילה הוא טקסט <b>קבוע</b> שמופיע בכל כישלון,
        /// ולכן אינו מבחין בין המקרים ובכל זאת נותן למורה שננעלה את הסיבה.
        /// </para>
        /// </summary>
        private static readonly string InvalidCredentialsMessage =
            "שם המשתמש או הסיסמה שגויים. " +
            $"לאחר {User.MaxFailedLoginAttempts} ניסיונות כושלים החשבון ננעל למשך " +
            $"{User.LockoutDuration.TotalMinutes:0} דקות.";

        private const string OrphanStudentAccountMessage =
            "החשבון שלך אינו מקושר לרשומת תלמידה במערכת, ולכן אין לו לאן להיכנס. " +
            "יש לפנות למורה כדי לקשר אותו.";

        private readonly IUserRepository _userRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IUnitOfWork _unitOfWork;

        public LoginHandler(
            IUserRepository userRepository,
            IStudentRepository studentRepository,
            IPasswordHasherService passwordHasher,
            IJwtTokenGenerator tokenGenerator,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _studentRepository = studentRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Dto.Username, cancellationToken);
            var now = DateTime.UtcNow;

            // Generic error — never reveal whether the username exists
            if (user is null)
                throw new BusinessRuleException(InvalidCredentialsMessage);

            // ⚠️ יציאה *לפני* ספירת הכישלון. ניסיון על חשבון שכבר נעול אינו מאריך את הנעילה,
            // אחרת תוקף שממשיך לנחש נועל חשבון של מורה לצמיתות.
            if (user.IsLockedOut(now))
                throw new BusinessRuleException(InvalidCredentialsMessage);

            if (!_passwordHasher.Verify(user.PasswordHash, request.Dto.Password))
            {
                user.RegisterFailedLogin(now);
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                throw new BusinessRuleException(InvalidCredentialsMessage);
            }

            // סיסמה נכונה מנקה את המונה — חמש טעויות הקלדה מפוזרות אינן אמורות להצטבר
            // לנעילה אחרי כניסה מוצלחת ביניהן.
            if (user.FailedLoginAttempts > 0 || user.LockoutEndsAt.HasValue)
            {
                user.RegisterSuccessfulLogin();
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            int? studentId = null;
            if (user.Role == UserRole.Student)
            {
                var student = await _studentRepository.GetByUserIdAsync(user.Id, cancellationToken);

                // ⚠️ עד לתיקון הזה studentId נשאר null, ה-claim הושמט מהטוקן, והתלמידה נכנסה
                // למסכים ריקים עם כפתור הגשה שאינו עושה כלום — ולולאת ניתוב בין "/" ל-homeRoute.
                // עדיף להיכשל כאן, פעם אחת, עם הסבר.
                if (student is null)
                    throw new BusinessRuleException(OrphanStudentAccountMessage);

                studentId = student.Id;
            }

            var token = _tokenGenerator.GenerateToken(user, studentId);

            return new AuthResponseDto(token, user.FullName, user.Role.ToString(), studentId);
        }
    }
}

using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Common.Security;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Auth.ResetPassword
{
    public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
    {
        /// <summary>
        /// ההודעה היחידה לכל מסלולי הכישלון: טוקן שאינו קיים, טוקן שפג, וטוקן שכבר נוצל.
        /// <para>
        /// ⚠️ אין לפצל אותה, מאותו נימוק בדיוק כמו ב-<c>LoginHandler</c>. "הקישור כבר נוצל"
        /// מאשר לקורא שהטוקן שבידיו אמיתי ושייך לחשבון קיים, ו-"הקישור פג" מאשר את אותו
        /// דבר ומוסיף מתי. הפעולה שהמשתמשת צריכה לעשות זהה בשלושת המקרים — לבקש קישור חדש —
        /// ולכן הפיצול לא היה נותן לה דבר והיה נותן למי שמנחשת הכול.
        /// </para>
        /// </summary>
        private const string InvalidTokenMessage =
            "הקישור אינו תקף יותר. ייתכן שפג תוקפו, שכבר נעשה בו שימוש, או שנשלח קישור חדש " +
            "אחריו. יש לבקש קישור חדש.";

        private readonly IPasswordResetTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public ResetPasswordHandler(
            IPasswordResetTokenRepository tokenRepository,
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var tokenHash = PasswordResetTokenGenerator.Hash(request.Dto.Token);

            var token = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (token is null || !token.IsUsable(now))
                throw new BusinessRuleException(InvalidTokenMessage);

            var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);

            // המשתמשת נמחקה בין שליחת הקישור ללחיצה עליו. אותה הודעה — אין סיבה לספר
            // למי שמחזיקה קישור ישן מה עלה בגורל החשבון.
            if (user is null)
                throw new BusinessRuleException(InvalidTokenMessage);

            user.SetPasswordHash(_passwordHasher.Hash(request.Dto.NewPassword));

            // ⚠️ שחרור הנעילה הוא חלק מהאיפוס ולא תוספת, בדיוק כמו ב-ResetTeacherPasswordHandler:
            // מי שלא זוכרת את הסיסמה שלה כבר ניסתה כמה פעמים, וסביר שהחשבון נעול. בלי זה היא
            // בוחרת סיסמה חדשה, חוזרת למסך הכניסה, ונדחית שוב בלי להבין למה.
            user.RegisterSuccessfulLogin();

            // ⚠️ מגבלה ידועה — איפוס סיסמה **אינו** מנתק session פתוח. אין במערכת מנגנון
            // ביטול ל-JWT, וטוקן שהונפק עם הסיסמה הישנה נשאר תקף עד לפקיעתו (Jwt:ExpiresHours,
            // 8 שעות). זה מקובל כאן: מודל האיום הוא סיסמה שנשכחה, לא session שנגנב. מי שתוסיף
            // בעתיד ביטול טוקנים — כאן המקום לקרוא לו, ואין להניח שהאיפוס כבר עושה את זה.
            token.MarkUsed(now);

            await _userRepository.UpdateAsync(user, cancellationToken);

            // שמירה אחת לשניהם: אם חותמת הניצול והסיסמה החדשה היו נשמרות בנפרד, כשל בין
            // השתיים היה משאיר קישור שמיש אחרי שהסיסמה כבר הוחלפה.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

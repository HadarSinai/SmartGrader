using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Auth.ChangeMyPassword
{
    public class ChangeMyPasswordHandler : IRequestHandler<ChangeMyPasswordCommand>
    {
        /// <summary>
        /// ⚠️ הודעה מפורשת, בשונה מ-<c>LoginHandler</c> ו-<c>ResetPasswordHandler</c>, ובכוונה.
        /// שם ההודעה הגנרית מסתירה <b>אילו חשבונות קיימים</b> ממי שאינה מזוהה. כאן הקוראת כבר
        /// מחוברת כבעלת החשבון — היא לא לומדת דבר שאינו שלה, ו-"משהו השתבש" היה משאיר אותה
        /// מנחשת אם טעתה בסיסמה הישנה או שהחדשה נפסלה.
        /// </summary>
        private const string WrongCurrentPasswordMessage =
            "הסיסמה הנוכחית שגויה. הסיסמה לא שונתה.";

        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public ChangeMyPasswordHandler(
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ChangeMyPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.CurrentUserId, cancellationToken);

            // המשתמשת נמחקה בזמן שה-session שלה עדיין פתוח.
            if (user is null)
                throw new NotFoundException("User", request.CurrentUserId);

            // ⚠️ אימות הסיסמה הנוכחית הוא **הבקרה כולה** בנקודה הזו, לא פורמליות. [Authorize]
            // מוודא רק שיש טוקן תקף; בלי הבדיקה הזו מי שמתיישבת מול מחשב שנשאר מחובר משנה
            // סיסמה בשתי לחיצות ונועלת את המורה מחוץ לחשבון שלה.
            if (!_passwordHasher.Verify(user.PasswordHash, request.Dto.CurrentPassword))
                throw new BusinessRuleException(WrongCurrentPasswordMessage);

            user.SetPasswordHash(_passwordHasher.Hash(request.Dto.NewPassword));

            // שחרור נעילה, מאותו נימוק כמו ב-ResetPasswordHandler: מונה כישלונות שנצבר מול
            // הסיסמה **הישנה** חסר משמעות אחרי שהיא הוחלפה, והיה נועל את בעלת החשבון בכניסה
            // הבאה שלה על ניחושים של מישהי אחרת.
            user.RegisterSuccessfulLogin();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ⚠️ אין כאן ניתוק של sessions אחרים — אין במערכת מנגנון ביטול ל-JWT, וטוקן שהונפק
            // עם הסיסמה הישנה נשאר תקף עד לפקיעתו (Jwt:ExpiresHours). זו אותה מגבלה בדיוק
            // שמתועדת ב-ResetPasswordHandler, ואין להניח ששינוי סיסמה מגרש פורץ שכבר נכנס.
        }
    }
}

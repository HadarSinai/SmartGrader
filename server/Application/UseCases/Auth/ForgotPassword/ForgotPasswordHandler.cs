using MediatR;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Common.Security;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Auth.ForgotPassword
{
    /// <summary>
    /// מקבלת כתובת מייל ושולחת אליה קישור איפוס חד-פעמי.
    /// <para>
    /// 🔴 העיקרון שמנהל את כל ה-handler: <b>התשובה זהה בכל מסלול.</b> כתובת רשומה, כתובת
    /// שאינה קיימת, חשבון תלמידה, ואפילו תקלת SMTP — כולן מחזירות 200 ריק. כל הבדל שהיה
    /// דולף החוצה, לרבות סטטוס שגיאה שונה, היה הופך את הנקודה למונה חשבונות רשומים:
    /// מי שמריצה עליה רשימת כתובות מקבלת בחזרה בדיוק מי מהן במערכת.
    /// </para>
    /// <para>
    /// ⚠️ מגבלה ידועה: זמן התגובה עדיין ארוך יותר עבור כתובת רשומה, כי רק היא מגיעה
    /// לשליחת מייל. ערוץ צדדי צר ורועש, ומדיניות הקצב "auth" מגבילה כמה מדידות אפשר
    /// לאסוף — התיקון (שליחה ברקע) לא נעשה כאן כדי לא לוותר על רישום תקלת ה-SMTP.
    /// </para>
    /// </summary>
    public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetTokenRepository _tokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly IClientUrlProvider _clientUrl;
        private readonly ILogWriter _logWriter;

        public ForgotPasswordHandler(
            IUserRepository userRepository,
            IPasswordResetTokenRepository tokenRepository,
            IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            IClientUrlProvider clientUrl,
            ILogWriter logWriter)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _clientUrl = clientUrl;
            _logWriter = logWriter;
        }

        public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Dto.Email, cancellationToken);
            var now = DateTime.UtcNow;

            // אין חשבון כזה — יוצאים בשקט. (משתמשת בלי מייל אינה יכולה להגיע לכאן ממילא:
            // ההשוואה היא Email == הכתובת, ו-NULL אינו שווה לשום ערך.)
            if (user is null)
                return;

            // ⚠️ תלמידה אינה משחזרת סיסמה במייל — אין לה כתובת, והמורה שלה מאפסת לה.
            // הבדיקה כאן היא הגנה מפני שורת תלמידה שקיבלה מייל בדרך אחרת: היא לא אמורה
            // לקבל מסלול כניסה שעוקף את המורה.
            if (user.Role == UserRole.Student)
                return;

            // תצורה חסרה = תקלה תפעולית, לא "אין חשבון". בלי הבדיקה הזו הקישור היה נשלח
            // כ-"/reset-password?token=..." בלי מארח — כתובת שבורה במייל שכבר יצא.
            if (!_clientUrl.IsConfigured)
            {
                await LogOperationalFaultAsync(
                    "כתובת הלקוח (App:ClientBaseUrl) אינה מוגדרת — לא ניתן לבנות קישור איפוס סיסמה",
                    user.Id,
                    cancellationToken);
                return;
            }

            // קישור חדש גובר על כל קישור פתוח. בלי זה מורה שביקשה קישור פעמיים מחזיקה שני
            // קישורים חיים במקביל, ואם הראשון הגיע לתיבה שאינה שלה הוא נשאר שמיש.
            await _tokenRepository.InvalidateAllForUserAsync(user.Id, now, cancellationToken);

            var rawToken = PasswordResetTokenGenerator.Generate();
            await _tokenRepository.AddAsync(
                PasswordResetToken.Create(user.Id, PasswordResetTokenGenerator.Hash(rawToken), now),
                cancellationToken);

            // ⚠️ שמירה *לפני* השליחה. בסדר ההפוך, כשל בשמירה היה משאיר במייל קישור שאין לו
            // שורה, כלומר מורה שלוחצת ומקבלת "הקישור אינו תקף" בלי סיבה נראית לעין.
            // הכיוון הזה נכשל לצד הבטוח: טוקן שנשמר ואיש אינו מחזיק פג מעצמו בתוך שעה.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resetUrl = $"{_clientUrl.BaseUrl}/reset-password?token={rawToken}";

            try
            {
                var sent = await _emailSender.SendAsync(
                    to: user.Email!,
                    subject: "SmartGrader – איפוס סיסמה",
                    body: BuildEmailBody(user.FullName, resetUrl),
                    ct: cancellationToken);

                if (!sent)
                {
                    await LogOperationalFaultAsync(
                        "SMTP אינו מוגדר — קישור לאיפוס סיסמה לא נשלח",
                        user.Id,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // 🔴 לא לתת לחריגה לצאת. חריגה מתורגמת ל-500, ו-500 על כתובת רשומה מול 200
                // על כתובת שאינה קיימת הוא בדיוק ההבדל שכל ה-handler נכתב כדי למחוק.
                await LogOperationalFaultAsync(
                    $"שליחת קישור לאיפוס סיסמה נכשלה: {ex.Message}",
                    user.Id,
                    cancellationToken);
            }
        }

        /// <summary>
        /// רושמת תקלה תפעולית ללוגים (Status=Error, ולכן מגיעה גם למסך הלוגים של המנהלת
        /// וגם להתראה במייל אליה).
        /// <para>
        /// ⚠️ זה כל מה שמפריד בין "המערכת שקטה" ל-"המערכת שבורה בשקט": הקוראת רואה תמיד
        /// "אם הכתובת רשומה, נשלח אליה קישור", ולכן אם התקלה לא נרשמת כאן אין לה שום עקבה.
        /// </para>
        /// </summary>
        private Task LogOperationalFaultAsync(string message, int userId, CancellationToken ct) =>
            _logWriter.WriteAsync(
                actionType: LogActionTypes.PasswordResetEmailFailed,
                message: message,
                status: LogStatuses.Error,
                systemSource: LogSystemSources.Api,
                userId: userId,
                ct: ct);

        private static string BuildEmailBody(string fullName, string resetUrl) =>
            $"""
             שלום {fullName},

             התקבלה בקשה לאיפוס הסיסמה שלך במערכת SmartGrader.
             לבחירת סיסמה חדשה יש להיכנס לקישור הבא:

             {resetUrl}

             הקישור תקף למשך שעה אחת בלבד, וניתן להשתמש בו פעם אחת.
             אם לא ביקשת לאפס את הסיסמה, אין צורך לעשות דבר — הסיסמה הנוכחית נשארת בתוקף.

             בהצלחה,
             מערכת SmartGrader
             """;
    }
}

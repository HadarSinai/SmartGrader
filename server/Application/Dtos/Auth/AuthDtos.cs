namespace SmartGrader.Application.Dtos.Auth
{
    public record LoginRequestDto(string Username, string Password);

    // ⚠️ אין כאן RegisterTeacherRequestDto. הרשמה עצמית נסגרה: חשבון מורה נוצר רק בידי
    // המנהלת דרך POST /api/teachers. שרשרת ההרשאות היא מנהלת → מורות → תלמידות, ואף אחת
    // לא נכנסת בלי שמישהי מעליה יצרה לה חשבון.
    public record CreateStudentAccountRequestDto(string FullName, int ClassId, string Username, string Password);

    public record CreateAccountForStudentRequestDto(string Username, string Password);

    public record ForgotPasswordRequestDto(string Email);

    public record ResetPasswordRequestDto(string Token, string NewPassword);

    // ── האזור האישי: משתמשת מתחזקת את החשבון של עצמה ──
    //
    // ⚠️ בשני ה-DTOs האלה **אין** שדה מזהה, וזו לא השמטה. המזהה נלקח תמיד מה-claims של
    // הטוקן (CurrentUserId ב-AuthController). ברגע שהיה כאן UserId, כל מורה מחוברת הייתה
    // יכולה לשנות את השם, המייל והסיסמה של כל משתמשת אחרת פשוט על ידי הצבת מזהה אחר בגוף
    // הבקשה — [Authorize] מוודא שהקוראת מחוברת, לא שהיא הבעלים של השורה.
    public record UpdateMyProfileRequestDto(string FullName, string Email);

    public record ChangeMyPasswordRequestDto(string CurrentPassword, string NewPassword);

    /// <summary>
    /// פרטי החשבון של המשתמשת המחוברת, כפי שהם **במסד** — לטעינת הטופס באזור האישי.
    /// <para>
    /// ⚠️ נפרד מ-<see cref="CurrentUserDto"/> ולא הרחבה שלו, וזו לא כפילות מיותרת:
    /// <c>CurrentUserDto</c> נבנה מה-claims של הטוקן בלבד ובלי פנייה למסד, והמייל אינו
    /// claim (ואינו אמור להיות — טוקן נשמר ב-localStorage וניתן לקריאה בקלות). הוספת המייל
    /// לשם הייתה מחייבת אחד משניים: קריאה למסד בנקודה שמסומנת במפורש כמהירה וחסרת-מסד,
    /// או פרט מזהה בתוך הטוקן.
    /// </para>
    /// <para>
    /// <c>PasswordHash</c> אינו כאן ולא ימופה לכאן לעולם — גם לא לבעלת החשבון עצמה.
    /// </para>
    /// </summary>
    public record MyProfileResponseDto(
        int UserId,
        string Username,
        string FullName,
        string? Email,
        string Role);

    // ⚠️ אין כאן ForgotPasswordResponseDto ואין ResetTokenDto. שתי הנקודות מחזירות גוף ריק
    // בכוונה: כל שדה שהיה חוזר מ-forgot-password — "נמצאה משתמשת", "נשלח מייל", ובוודאי
    // הטוקן עצמו — היה הופך את הנקודה למונה חשבונות רשומים, או מדלג על תיבת המייל לגמרי.

    public record AuthResponseDto(string Token, string FullName, string Role, int? StudentId);

    public record CurrentUserDto(int UserId, string FullName, string Role, int? StudentId);
}

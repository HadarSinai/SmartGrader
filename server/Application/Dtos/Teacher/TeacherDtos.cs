namespace SmartGrader.Application.Dtos.Teacher
{
    /// <summary>
    /// ⚠️ <c>PasswordHash</c> אינו מופיע כאן ולא ימופה לכאן לעולם — גם לא למנהלת.
    /// ר' <c>.claude/skills/backend-role-based-field-redaction</c>.
    /// <para>
    /// <see cref="Email"/> nullable כאן ולא בבקשות: שורות מורות שנוצרו לפני עמודת המייל
    /// מגיעות בלי מייל, והמסך מסמן אותן. כתיבה חדשה חייבת לספק מייל.
    /// </para>
    /// </summary>
    public record TeacherResponseDto(
        int Id,
        string FullName,
        string Username,
        string? Email,
        DateTime CreatedAt,
        int LessonsCount,
        int CoursesCount);

    public record CreateTeacherRequestDto(string FullName, string Username, string Email, string Password);

    /// <summary>שם המשתמש אינו כאן — הוא מזהה ההתחברות ואינו ניתן לשינוי.</summary>
    public record UpdateTeacherRequestDto(string FullName, string Email);

    public record ResetTeacherPasswordRequestDto(string NewPassword);
}

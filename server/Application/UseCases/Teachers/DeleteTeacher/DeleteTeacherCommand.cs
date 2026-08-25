using MediatR;

namespace SmartGrader.Application.UseCases.Teachers.DeleteTeacher
{
    /// <summary>
    /// <paramref name="CurrentUserId"/> מגיע מה-claims ב-controller ולא מהגוף של הבקשה —
    /// הוא קיים כאן רק כדי לחסום מנהלת שמוחקת את עצמה.
    /// </summary>
    public record DeleteTeacherCommand(int Id, int CurrentUserId) : IRequest;
}

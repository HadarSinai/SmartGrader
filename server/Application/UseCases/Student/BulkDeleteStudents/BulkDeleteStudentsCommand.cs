using MediatR;
using SmartGrader.Application.Dtos.Common;

namespace SmartGrader.Application.UseCases.Students.BulkDeleteStudents
{
    /// <remarks>
    /// ⚠️ בלי <c>TeacherId</c>, בדיוק כמו <c>DeleteStudentCommand</c>: תלמידה היא משאב
    /// מוסדי משותף ולא נכס של מורה. שינוי הכלל הזה כאן היה יוצר משאב שמחיקתו הבודדת
    /// והמרובה מצייתות לשתי מדיניות שונות.
    /// </remarks>
    public record BulkDeleteStudentsCommand(
        IReadOnlyList<int> StudentIds) : IRequest<BulkDeleteResultDto>;
}

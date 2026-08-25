using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Authorization
{
    /// <summary>
    /// "אילו תלמידות המורה הזו רואה" — הגדרה אחת, לרשימה ולייצוא כאחד.
    /// <para>
    /// 🔴 הפער שזה סוגר: <c>IStudentRepository</c> נושא אזהרה מפורשת שדוחות וייצוא חייבים
    /// לעבור דרך <c>GetByClassIdsAsync</c>, "אחרת כל מורה מייצאת רשימה ובה כל תלמידה בבית
    /// הספר" — ובדיוק שני הקוראים האלה קראו ל-<c>GetAllAsync</c>.
    /// </para>
    /// <para>
    /// ⚠️ ל-<c>Student</c> אין <c>TeacherId</c>, ולכן הבעלות נגזרת בעקיפין: השיעורים
    /// שבבעלות המורה → הכיתות המשויכות להם → התלמידות בכיתות. זו אותה הגדרה שדוח התקופה
    /// (<c>ExportGradesPeriodReportHandler</c>) כבר משתמש בה, ולא הגדרת בעלות שנייה.
    /// </para>
    /// </summary>
    public static class StudentScope
    {
        /// <param name="teacherId">
        /// <c>null</c> = מנהל/ת, בלי סינון — אותה פרצת מילוט שכל שאר ה-handlers משתמשים בה
        /// (<c>OwnerScopeTeacherId</c>).
        /// </param>
        public static async Task<IReadOnlyList<Student>> GetVisibleAsync(
            IStudentRepository students,
            ILessonRepository lessons,
            int? teacherId,
            bool includeArchived,
            bool includeCounts,
            CancellationToken ct)
        {
            if (teacherId is null)
                return await students.GetAllAsync(includeArchived, ct);

            var ownLessons = await lessons.GetAllAsync(classId: null, teacherId: teacherId, ct);

            var classIds = ownLessons
                .SelectMany(l => l.Classes)
                .Select(c => c.Id)
                .Distinct()
                .ToList();

            // מורה בלי שיעורים מקבלת רשימה ריקה, ולא את כל בית הספר: GetByClassIdsAsync
            // מחזירה ריק עבור רשימת כיתות ריקה.
            return await students.GetByClassIdsAsync(classIds, includeArchived, includeCounts, ct);
        }
    }
}

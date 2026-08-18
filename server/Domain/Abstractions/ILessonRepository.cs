using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface ILessonRepository
    {
        // ⚠️ אין עומס יתר (overload) חסר-teacherId בכוונה — זה בדיוק החור שדרכו ExportGradesPeriodReport/
        // GetStudentGradesSummary דלפו נתונים בין מורים. כל קריאה חייבת להעביר teacherId (null = מנהל/ת).
        Task<IReadOnlyList<Lesson>> GetAllAsync(int? classId, int? teacherId, CancellationToken ct = default);
        Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<Lesson>> GetByDateRangeAsync(DateTime from, DateTime to, int? teacherId, CancellationToken ct = default);
        Task AddAsync(Lesson lesson, CancellationToken ct = default);
        //Task UpdateAsync(Lesson lesson, CancellationToken ct = default);
        Task DeleteAsync(Lesson lesson, CancellationToken ct = default);
    }
}


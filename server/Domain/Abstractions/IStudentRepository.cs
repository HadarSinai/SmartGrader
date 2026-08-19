using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Domain.Abstractions
{
    public interface IStudentRepository
    {
        Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Student>> GetAllAsync(bool includeArchived, CancellationToken ct = default);
        // ⚠️ GetAllAsync() ללא פרמטרים מחזירה את כל בית הספר, כולל כיתות בארכיון — היא מיועדת
        // למסכי ניהול בלבד. לדוחות ולייצוא יש להשתמש ב-GetByClassIdsAsync, אחרת כל מורה מייצאת
        // רשימה ובה כל תלמידה בבית הספר.
        Task<IReadOnlyList<Student>> GetByClassIdsAsync(
            IReadOnlyList<int> classIds, bool includeArchived, CancellationToken ct = default);
        Task<Student?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Student?> GetByUserIdAsync(int userId, CancellationToken ct = default);
        Task AddAsync(Student student, CancellationToken ct = default);
        Task UpdateAsync(Student student, CancellationToken ct = default);
        Task DeleteAsync(Student student, CancellationToken ct = default);
    }
}

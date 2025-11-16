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
        Task<IReadOnlyList<Lesson>> GetAllAsync(CancellationToken ct = default);
        Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default);
        Task AddAsync(Lesson lesson, CancellationToken ct = default);
        Task UpdateAsync(Lesson lesson, CancellationToken ct = default);
        Task DeleteAsync(Lesson lesson, CancellationToken ct = default);
    }
}


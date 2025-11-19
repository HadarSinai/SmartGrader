using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Abstractions
{
    public interface IStudentRepository
    {
        Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default);
        Task<Student?> GetByIdAsync(int id, CancellationToken ct = default);
        Task AddAsync(Student student, CancellationToken ct = default);
        Task UpdateAsync(Student student, CancellationToken ct = default);
        Task DeleteAsync(Student student, CancellationToken ct = default);
    }
}

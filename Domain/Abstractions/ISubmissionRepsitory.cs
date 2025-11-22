using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Domain.Abstractions
{
    public interface ISubmissionRepository
    {
        Task<IReadOnlyList<Submission>> GetAllAsync(CancellationToken ct = default);
        Task<Submission?> GetByIdAsync(int id, CancellationToken ct = default);
        Task AddAsync(Submission submission, CancellationToken ct = default);
        Task UpdateAsync(Submission submission, CancellationToken ct = default);
        Task DeleteAsync(Submission submission, CancellationToken ct = default);
    }
}

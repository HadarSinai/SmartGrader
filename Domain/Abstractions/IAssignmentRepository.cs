using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface IAssignmentRepository
    {
        Task<Assignment?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<Assignment>> GetByLessonIdAsync(int lessonId, CancellationToken ct = default);
        Task AddAsync(Assignment assignment, CancellationToken ct = default);
        Task UpdateAsync(Assignment assignment, CancellationToken ct = default);
        Task DeleteAsync(Assignment assignment, CancellationToken ct = default);
    }
}

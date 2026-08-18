using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface ICourseRepository
    {
        Task<IReadOnlyList<Course>> GetAllAsync(int? teacherId, CancellationToken ct = default);
        Task<Course?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Course?> GetByNameAndTeacherAsync(string name, int teacherId, CancellationToken ct = default);
        Task AddAsync(Course course, CancellationToken ct = default);
        Task DeleteAsync(Course course, CancellationToken ct = default);
    }
}

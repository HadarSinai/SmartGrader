// SmartGrader.Domain/Abstractions/ILessonResultRepository.cs
using SmartGrader.Domain.Entities;
namespace SmartGrader.Domain.Abstractions;
public interface ILessonResultRepository
{
    Task<LessonResult?> GetAsync(int studentId, int lessonId, CancellationToken ct = default);
    Task<IReadOnlyList<LessonResult>> GetByLessonIdAsync(int lessonId, CancellationToken ct = default);
    Task<IReadOnlyList<LessonResult>> GetByLessonIdsAsync(IReadOnlyList<int> lessonIds, CancellationToken ct = default);
    Task<IReadOnlyList<LessonResult>> GetByStudentIdAsync(int studentId, CancellationToken ct = default);
    Task AddAsync(LessonResult entity, CancellationToken ct = default);
}

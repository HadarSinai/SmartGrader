using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface ISchoolClassRepository
    {
        Task<IReadOnlyList<SchoolClass>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default);
        Task<SchoolClass?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<SchoolClass?> GetByNameAndYearAsync(string name, int academicYear, CancellationToken ct = default);
        Task<IReadOnlyList<SchoolClass>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
        Task AddAsync(SchoolClass schoolClass, CancellationToken ct = default);
        Task UpdateAsync(SchoolClass schoolClass, CancellationToken ct = default);
        Task DeleteAsync(SchoolClass schoolClass, CancellationToken ct = default);
        Task<int> ArchiveAllActiveAsync(CancellationToken ct = default);
    }
}

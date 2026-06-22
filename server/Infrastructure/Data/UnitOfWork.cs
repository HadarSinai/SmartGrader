using Infrastructure.Data;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly GradeSheetContext _db;
    public UnitOfWork(GradeSheetContext db) => _db = db;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}



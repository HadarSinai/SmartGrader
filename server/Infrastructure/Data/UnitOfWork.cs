
using Microsoft.EntityFrameworkCore;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly GradeSheetContext _db;
    public UnitOfWork(GradeSheetContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            if (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("constraint failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new UniqueConstraintException("Duplicate value – record already exists.");
            }
            throw;
        }
    }
}



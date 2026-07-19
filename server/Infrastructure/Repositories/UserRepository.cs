using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;

namespace SmartGrader.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly GradeSheetContext _context;

        public UserRepository(GradeSheetContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            var normalized = username.Trim().ToLowerInvariant();
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == normalized, ct);
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, ct);
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default)
        {
            var normalized = username.Trim().ToLowerInvariant();
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Username == normalized, ct);
        }

        public async Task AddAsync(User user, CancellationToken ct = default)
        {
            await _context.Users.AddAsync(user, ct);
        }

        public Task DeleteAsync(User user, CancellationToken ct = default)
        {
            _context.Users.Remove(user);
            return Task.CompletedTask;
        }
    }
}

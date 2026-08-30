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

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalized, ct);
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default)
        {
            var normalized = username.Trim().ToLowerInvariant();
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Username == normalized, ct);
        }

        public async Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .OrderBy(u => u.FullName)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<User>> GetByRoleWithoutEmailAsync(UserRole role, CancellationToken ct = default)
        {
            // מייל ריק ומייל NULL הם אותה תקלה מבחינת השחזור: GetByEmailAsync משווה למחרוזת
            // מנורמלת ולא מתאימה לאף אחד מהם.
            return await _context.Users
                .Where(u => u.Role == role && (u.Email == null || u.Email == ""))
                .OrderBy(u => u.Username)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludingUserId, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();

            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Email == normalized);

            if (excludingUserId.HasValue)
                query = query.Where(u => u.Id != excludingUserId.Value);

            return await query.AnyAsync(ct);
        }

        public async Task AddAsync(User user, CancellationToken ct = default)
        {
            await _context.Users.AddAsync(user, ct);
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _context.Users.Update(user);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(User user, CancellationToken ct = default)
        {
            _context.Users.Remove(user);
            return Task.CompletedTask;
        }
    }
}

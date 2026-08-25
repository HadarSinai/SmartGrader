using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;

namespace SmartGrader.Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly GradeSheetContext _context;

        public PasswordResetTokenRepository(GradeSheetContext context)
        {
            _context = context;
        }

        // ⚠️ בכוונה בלי AsNoTracking — ר' ההסבר בממשק.
        public async Task<PasswordResetToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken ct = default)
        {
            return await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        }

        public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
        {
            await _context.PasswordResetTokens.AddAsync(token, ct);
        }

        public async Task InvalidateAllForUserAsync(
            int userId,
            DateTime utcNow,
            CancellationToken ct = default)
        {
            var outstanding = await _context.PasswordResetTokens
                .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > utcNow)
                .ToListAsync(ct);

            foreach (var token in outstanding)
                token.MarkUsed(utcNow);
        }
    }
}

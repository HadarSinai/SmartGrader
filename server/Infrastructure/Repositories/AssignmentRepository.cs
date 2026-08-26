using Microsoft.EntityFrameworkCore;
using SmartGrader.Infrastructure.Data;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;


namespace SmartGrader.Infrastructure.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private readonly GradeSheetContext _context;

        public AssignmentRepository(GradeSheetContext context)
        {
            _context = context;
        }

        public async Task<Assignment?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Assignments
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        // ⚠️ ה-Include על Submissions אינו קישוט: AssignmentProfile ממפה את SubmissionsCount
        // לפי מוסכמת השם מ-Submissions.Count, ובלי הטעינה הרשימה תמיד ריקה — עמודת "הגשות"
        // במסך התרגילים הראתה 0 לכל תרגיל, גם כשהיו עשרות הגשות. GetByIdAsync כבר טוען אותן,
        // ולכן אותו תרגיל הראה מספר נכון במסך אחד ואפס בשני.
        public async Task<IReadOnlyList<Assignment>> GetByLessonIdAsync(int lessonId, CancellationToken ct = default)
        {
            return await _context.Assignments
                .Where(a => a.LessonId == lessonId)
                .Include(a => a.Submissions)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task AddAsync(Assignment assignment, CancellationToken ct = default)
        {
            await _context.Assignments.AddAsync(assignment, ct);
        }

        //public async Task UpdateAsync(Assignment assignment, CancellationToken ct = default)
        //{
        //    _context.Assignments.Attach(assignment);
        //}

        public async Task DeleteAsync(Assignment assignment, CancellationToken ct = default)
        {
            _context.Assignments.Remove(assignment);
        }
    }
}


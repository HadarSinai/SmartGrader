using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Infrastructure.Repositories
{
    public class LessonResultRepository : ILessonResultRepository
    {
        private readonly GradeSheetContext _db;

        public LessonResultRepository(GradeSheetContext db)
        {
            _db = db;
        }

        public async Task<LessonResult?> GetAsync(int studentId, int lessonId, CancellationToken ct = default)
        {
            return await _db.LessonResults
                .FirstOrDefaultAsync(x => x.StudentId == studentId && x.LessonId == lessonId, ct);
        }

        public async Task AddAsync(LessonResult entity, CancellationToken ct = default)
        {
            await _db.LessonResults.AddAsync(entity, ct);
        }
    }
}


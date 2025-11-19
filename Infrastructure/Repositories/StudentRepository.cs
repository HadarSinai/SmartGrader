using Domain.Abstractions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly GradeSheetContext _context;
        public StudentRepository(GradeSheetContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Students
                .AsNoTracking() 
                .ToListAsync();
        }
        public async Task<Student?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }
        public async Task AddAsync(Student student, CancellationToken ct = default)
        {
            await _context.Students.AddAsync(student, ct);
        }
        public Task UpdateAsync(Student student, CancellationToken ct = default)
        {
            _context.Students.Update(student);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Student student, CancellationToken ct = default)
        {
            _context.Students.Remove(student);
            return Task.CompletedTask;
        }
    }
}

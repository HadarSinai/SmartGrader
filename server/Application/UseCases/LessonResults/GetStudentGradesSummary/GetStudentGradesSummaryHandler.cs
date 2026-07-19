using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.HebrewDate;
using SmartGrader.Application.Dtos.LessonResults;
using SmartGrader.Domain.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.LessonResults.GetStudentGradesSummary;

public class GetStudentGradesSummaryHandler : IRequestHandler<GetStudentGradesSummaryQuery, StudentGradesSummaryDto>
{
    private readonly IStudentRepository _studentRepo;
    private readonly ILessonRepository _lessonRepo;
    private readonly ILessonResultRepository _lessonResultRepo;

    public GetStudentGradesSummaryHandler(
        IStudentRepository studentRepo,
        ILessonRepository lessonRepo,
        ILessonResultRepository lessonResultRepo)
    {
        _studentRepo = studentRepo;
        _lessonRepo = lessonRepo;
        _lessonResultRepo = lessonResultRepo;
    }

    public async Task<StudentGradesSummaryDto> Handle(GetStudentGradesSummaryQuery request, CancellationToken ct)
    {
        var student = await _studentRepo.GetByIdAsync(request.StudentId, ct);
        if (student is null)
            throw new NotFoundException("Student", request.StudentId);

        var results = await _lessonResultRepo.GetByStudentIdAsync(request.StudentId, ct);
        var lessons = await _lessonRepo.GetAllAsync(ct);
        var lessonsById = lessons.ToDictionary(l => l.Id);

        var grades = results
            .Where(r => lessonsById.ContainsKey(r.LessonId))
            .Select(r =>
            {
                var lesson = lessonsById[r.LessonId];
                return new StudentGradeItemDto
                {
                    LessonId = lesson.Id,
                    LessonName = lesson.Name,
                    LessonDateHebrew = HebrewDateConverter.ToHebrewString(lesson.LessonDate),
                    FinalScore = r.FinalScore,
                    IsComplete = r.IsComplete
                };
            })
            .OrderBy(g => lessonsById[g.LessonId].LessonDate)
            .ToList();

        var scores = grades.Where(g => g.FinalScore is not null).Select(g => g.FinalScore!.Value).ToList();

        return new StudentGradesSummaryDto
        {
            StudentId = student.Id,
            StudentName = student.FullName,
            Average = scores.Count > 0 ? Math.Round(scores.Average(), 1) : null,
            Grades = grades
        };
    }
}

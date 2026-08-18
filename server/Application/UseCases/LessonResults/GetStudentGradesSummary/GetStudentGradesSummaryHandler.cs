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
        var lessons = await _lessonRepo.GetAllAsync(classId: null, request.TeacherId, ct);
        var lessonsById = lessons.ToDictionary(l => l.Id);

        var courses = results
            .Where(r => lessonsById.ContainsKey(r.LessonId))
            .Select(r => new { Result = r, Lesson = lessonsById[r.LessonId] })
            .GroupBy(x => x.Lesson.CourseId)
            .Select(g =>
            {
                var grades = g
                    .Select(x => new StudentGradeItemDto
                    {
                        LessonId = x.Lesson.Id,
                        Subject = x.Lesson.Subject,
                        LessonDateHebrew = HebrewDateConverter.ToHebrewString(x.Lesson.LessonDate),
                        FinalScore = x.Result.FinalScore,
                        IsComplete = x.Result.IsComplete
                    })
                    .OrderBy(item => lessonsById[item.LessonId].LessonDate)
                    .ToList();

                var scores = grades.Where(gr => gr.FinalScore is not null).Select(gr => gr.FinalScore!.Value).ToList();

                return new CourseAverageDto
                {
                    CourseId = g.Key,
                    CourseName = g.First().Lesson.Course.Name,
                    Average = scores.Count > 0 ? Math.Round(scores.Average(), 1) : null,
                    Grades = grades
                };
            })
            .OrderBy(c => c.CourseName)
            .ToList();

        return new StudentGradesSummaryDto
        {
            StudentId = student.Id,
            StudentName = student.FullName,
            Courses = courses
        };
    }
}

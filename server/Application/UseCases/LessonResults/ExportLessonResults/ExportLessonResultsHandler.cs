using ClosedXML.Excel;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.LessonResults.ExportLessonResults;

public class ExportLessonResultsHandler : IRequestHandler<ExportLessonResultsQuery, byte[]>
{
    private readonly ILessonRepository _lessonRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly ISubmissionRepository _submissionRepo;
    private readonly ILessonResultRepository _lessonResultRepo;

    public ExportLessonResultsHandler(
        ILessonRepository lessonRepo,
        IStudentRepository studentRepo,
        IAssignmentRepository assignmentRepo,
        ISubmissionRepository submissionRepo,
        ILessonResultRepository lessonResultRepo)
    {
        _lessonRepo = lessonRepo;
        _studentRepo = studentRepo;
        _assignmentRepo = assignmentRepo;
        _submissionRepo = submissionRepo;
        _lessonResultRepo = lessonResultRepo;
    }

    public async Task<byte[]> Handle(ExportLessonResultsQuery request, CancellationToken ct)
    {
        var lesson = await LessonAccess.GetOwnedOrThrowAsync(_lessonRepo, request.LessonId, request.TeacherId, ct);

        var assignments = await _assignmentRepo.GetByLessonIdAsync(request.LessonId, ct);
        var total = assignments.Count;

        // ⚠️ לא GetAllAsync: היא מחזירה את כל תלמידות בית הספר, כולל כיתות בארכיון, כך שהייצוא
        // של מורה אחת חשף את שמות כל התלמידות במערכת. הרשימה מצומצמת לכיתות שהשיעור משויך אליהן.
        var classIds = lesson.Classes.Select(c => c.Id).ToList();
        var students = await _studentRepo.GetByClassIdsAsync(classIds, includeArchived: false, ct);

        var results = await _lessonResultRepo.GetByLessonIdAsync(request.LessonId, ct);
        var resultsByStudent = results.ToDictionary(r => r.StudentId);

        // שאילתה אחת לכל השיעור במקום GetByStudentAndLessonAsync לכל תלמידה בנפרד (N+1)
        var completedByStudent = (await _submissionRepo.GetByLessonIdAsync(request.LessonId, ct))
            .Where(s => s.Status == SubmissionStatus.Done)
            .GroupBy(s => s.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.AssignmentId).Distinct().Count());

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("ציונים");
        ws.RightToLeft = true;

        ws.Cell(1, 1).Value = "שם תלמידה";
        ws.Cell(1, 2).Value = "תרגילים שהושלמו";
        ws.Cell(1, 3).Value = "ציון סופי";
        ws.Cell(1, 4).Value = "סטטוס";
        ws.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var student in students)
        {
            completedByStudent.TryGetValue(student.Id, out var completed);

            resultsByStudent.TryGetValue(student.Id, out var result);

            ws.Cell(row, 1).Value = student.FullName;
            ws.Cell(row, 2).Value = $"{completed}/{total}";
            if (result?.FinalScore is not null)
                ws.Cell(row, 3).Value = result.FinalScore.Value;
            ws.Cell(row, 4).Value = result is not null && result.IsComplete ? "הושלם" : "בתהליך";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

using ClosedXML.Excel;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.HebrewDate;
using SmartGrader.Domain.Abstractions;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.LessonResults.ExportGradesPeriodReport;

public class ExportGradesPeriodReportHandler : IRequestHandler<ExportGradesPeriodReportQuery, byte[]>
{
    private readonly ILessonRepository _lessonRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly ILessonResultRepository _lessonResultRepo;

    public ExportGradesPeriodReportHandler(
        ILessonRepository lessonRepo,
        IStudentRepository studentRepo,
        ILessonResultRepository lessonResultRepo)
    {
        _lessonRepo = lessonRepo;
        _studentRepo = studentRepo;
        _lessonResultRepo = lessonResultRepo;
    }

    public async Task<byte[]> Handle(ExportGradesPeriodReportQuery request, CancellationToken ct)
    {
        var from = HebrewDateConverter.ToGregorian(request.FromHebrewYear, request.FromHebrewMonth, request.FromHebrewDay);
        var to = HebrewDateConverter.ToGregorian(request.ToHebrewYear, request.ToHebrewMonth, request.ToHebrewDay)
            .AddDays(1).AddTicks(-1);

        if (from > to)
            throw new BusinessRuleException("תאריך ההתחלה חייב להיות לפני תאריך הסיום");

        var lessons = await _lessonRepo.GetByDateRangeAsync(from, to, request.TeacherId, ct);
        if (lessons.Count == 0)
            throw new BusinessRuleException("אין שיעורים בתקופה שנבחרה");

        // ⚠️ לא GetAllAsync: היא מחזירה את כל תלמידות בית הספר, כולל שנים בארכיון. הדוח מצומצם
        // לכיתות שהשיעורים בתקופה משויכים אליהן, ובלי תלמידות מכיתות שהועברו לארכיון.
        var classIds = lessons.SelectMany(l => l.Classes).Select(c => c.Id).Distinct().ToList();
        var students = await _studentRepo.GetByClassIdsAsync(classIds, includeArchived: false, ct);

        var lessonIds = lessons.Select(l => l.Id).ToList();
        var results = await _lessonResultRepo.GetByLessonIdsAsync(lessonIds, ct);
        var resultsByStudentAndLesson = results.ToDictionary(r => (r.StudentId, r.LessonId));

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("ציונים סופיים");
        ws.RightToLeft = true;

        ws.Cell(1, 1).Value = "שם תלמידה";
        var lessonColumn = 2;
        foreach (var lesson in lessons)
        {
            ws.Cell(1, lessonColumn).Value = $"{lesson.Course.Name} ({HebrewDateConverter.ToHebrewString(lesson.LessonDate)})";
            lessonColumn++;
        }
        var averageColumn = lessonColumn;
        ws.Cell(1, averageColumn).Value = "ממוצע";
        ws.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var student in students)
        {
            ws.Cell(row, 1).Value = student.FullName;

            var studentScores = new List<double>();
            lessonColumn = 2;
            foreach (var lesson in lessons)
            {
                resultsByStudentAndLesson.TryGetValue((student.Id, lesson.Id), out var result);
                if (result?.FinalScore is not null)
                {
                    ws.Cell(row, lessonColumn).Value = result.FinalScore.Value;
                    studentScores.Add(result.FinalScore.Value);
                }
                lessonColumn++;
            }

            var averageCell = ws.Cell(row, averageColumn);
            if (studentScores.Count > 0)
                averageCell.Value = Math.Round(studentScores.Average(), 1);
            averageCell.Style.Font.Bold = true;

            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

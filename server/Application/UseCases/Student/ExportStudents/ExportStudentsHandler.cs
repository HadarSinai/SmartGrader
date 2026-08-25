using ClosedXML.Excel;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Domain.Abstractions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.ExportStudents;

public class ExportStudentsHandler : IRequestHandler<ExportStudentsQuery, byte[]>
{
    private readonly IStudentRepository _repository;
    private readonly ILessonRepository _lessons;

    public ExportStudentsHandler(IStudentRepository repository, ILessonRepository lessons)
    {
        _repository = repository;
        _lessons = lessons;
    }

    public async Task<byte[]> Handle(ExportStudentsQuery request, CancellationToken ct)
    {
        // ⚠️ לא GetAllAsync: זה בדיוק המקרה שהאזהרה ב-IStudentRepository מתארת — קובץ אחד
        // ובו כל תלמידה בבית הספר, בידי כל מורה שלוחצת "ייצוא". ר' StudentScope.
        // includeArchived נשאר true: ייצוא רשימת התלמידות משקף את המסך, שמציג גם ארכיון.
        var students = await StudentScope.GetVisibleAsync(
            _repository,
            _lessons,
            request.TeacherId,
            includeArchived: true,
            includeCounts: true,
            ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("סטודנטים");
        ws.RightToLeft = true;

        ws.Cell(1, 1).Value = "שם מלא";
        ws.Cell(1, 2).Value = "כיתה";
        ws.Cell(1, 3).Value = "מס' הגשות";
        ws.Cell(1, 4).Value = "מס' ציונים";
        ws.Cell(1, 5).Value = "יש חשבון";
        ws.Cell(1, 6).Value = "תאריך יצירה";
        ws.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var student in students)
        {
            ws.Cell(row, 1).Value = student.FullName;
            ws.Cell(row, 2).Value = student.Class?.Name ?? "";
            ws.Cell(row, 3).Value = student.Submissions?.Count ?? 0;
            ws.Cell(row, 4).Value = student.LessonResults?.Count ?? 0;
            ws.Cell(row, 5).Value = student.UserId != null ? "כן" : "לא";
            ws.Cell(row, 6).Value = student.CreatedAt.ToString("dd/MM/yyyy");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

---
name: backend-excel-closedxml-pattern
description: "Use when generating or parsing Excel (.xlsx) files in the SmartGrader .NET backend with ClosedXML: RTL Hebrew worksheets, bold header rows, byte[] export handlers returned via File(...), parsing uploaded xlsx with per-row validation and row-numbered errors, and the IFormFile→Stream boundary (Application never references AspNetCore). USE FOR: 'export X to excel', 'add an xlsx download endpoint', 'import/parse an uploaded excel file', 'build a ClosedXML worksheet'. NOT for the CQRS handler shell itself (see backend-mediatr-query-handler-pattern) or client-side download/upload (see client-file-download-upload-pattern)."
---

# Backend Excel (ClosedXML) Pattern

Server-side Excel generation and parsing with **ClosedXML** (in `server/Application/Application.csproj` only). Export use cases return `byte[]` through MediatR; controllers wrap them in `File(...)`. Import receives a `Stream` — never `IFormFile` — because the Application layer must not reference AspNetCore.

## When to Use

- Adding an "ייצוא" endpoint for a list/table (students, grades, any entity).
- Adding an "ייבוא" endpoint that parses rows with partial-success semantics.
- Reviewing xlsx handling for RTL Hebrew correctness or layer boundaries.

## Workflow — Export

1. Query record returns bytes: `public record ExportStudentsQuery() : IRequest<byte[]>;`
2. Handler builds the workbook (real code from `ExportStudentsHandler`):

```csharp
using var workbook = new XLWorkbook();
var ws = workbook.Worksheets.Add("סטודנטים");
ws.RightToLeft = true;                       // Hebrew RTL — required

ws.Cell(1, 1).Value = "שם מלא";
ws.Cell(1, 2).Value = "כיתה";
ws.Row(1).Style.Font.Bold = true;            // bold header row

var row = 2;
foreach (var student in students)
{
    ws.Cell(row, 1).Value = student.FullName;
    ws.Cell(row, 2).Value = student.ClassName;
    ws.Cell(row, 6).Value = student.CreatedAt.ToString("dd/MM/yyyy");
    row++;
}

ws.Columns().AdjustToContents();

using var stream = new MemoryStream();
workbook.SaveAs(stream);
return stream.ToArray();
```

3. Controller action (from `StudentsController.Export`):

```csharp
[HttpGet("export")]
[Authorize(Roles = "Teacher")]
public async Task<IActionResult> Export(CancellationToken cancellationToken)
{
    byte[] bytes = await _mediator.Send(new ExportStudentsQuery(), cancellationToken);
    return File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "students.xlsx");
}
```

## Workflow — Import (partial success)

1. Command takes a `Stream`, not `IFormFile`: `public record ImportStudentsCommand(Stream FileStream) : IRequest<ImportStudentsResultDto>;`
2. Result DTO: `{ int CreatedCount; List<ImportRowErrorDto> Errors }` with `{ int RowNumber; string Message }`.
3. Handler (from `ImportStudentsHandler`): open workbook in try/catch → `BusinessRuleException` (→ 400) on corrupt file; iterate `ws.RowsUsed().Skip(1)` (skip header); `row.Cell(1).GetString().Trim()` + `row.RowNumber()`; validate each row mirroring the entity's Create validator; collect errors and continue (partial success — never all-or-nothing); skip fully-empty rows silently; `SaveChangesAsync` once at the end only if `createdCount > 0`.
4. Controller converts `IFormFile` → `Stream` at the boundary (from `StudentsController.Import`):

```csharp
[HttpPost("import")]
[Authorize(Roles = "Teacher")]
public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
{
    const long maxFileSize = 5 * 1024 * 1024;

    if (file is null || file.Length == 0)
        return BadRequest(new { message = "לא נבחר קובץ להעלאה" });
    if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        return BadRequest(new { message = "יש להעלות קובץ Excel בפורמט xlsx בלבד" });
    if (file.Length > maxFileSize)
        return BadRequest(new { message = "הקובץ גדול מדי (מקסימום 5MB)" });

    await using var stream = file.OpenReadStream();
    ImportStudentsResultDto result =
        await _mediator.Send(new ImportStudentsCommand(stream), cancellationToken);
    return Ok(result);
}
```

## Pitfalls

- Don't forget `ws.RightToLeft = true;` — without it Hebrew sheets open LTR in Excel.
- Don't reference `IFormFile`/AspNetCore in Application — the controller owns file validation (extension, size) and passes a `Stream`.
- Don't roll back all rows on a bad row — report `{ RowNumber, Message }` and keep going.
- Don't compute counts from lazy navigations — ensure the repository `.Include(...)`s the collections the export needs (e.g. `Submissions`, `LessonResults`).
- Route note: `[HttpGet("export")]` coexists safely with `[HttpGet("{id:int}")]` thanks to the `:int` constraint.

## Real Files

- [ExportStudentsHandler.cs](../../../server/Application/UseCases/Student/ExportStudents/ExportStudentsHandler.cs)
- [ExportLessonResultsHandler.cs](../../../server/Application/UseCases/LessonResults/ExportLessonResults/ExportLessonResultsHandler.cs)
- [ImportStudentsHandler.cs](../../../server/Application/UseCases/Student/ImportStudents/ImportStudentsHandler.cs)
- [StudentsController.cs](../../../server/Api/Controllers/StudentsController.cs) / [LessonResultController.cs](../../../server/Api/Controllers/LessonResultController.cs)

## See Also

- [backend-mediatr-query-handler-pattern](../backend-mediatr-query-handler-pattern/SKILL.md) — the CQRS shell.
- [client-file-download-upload-pattern](../client-file-download-upload-pattern/SKILL.md) — the Angular side.

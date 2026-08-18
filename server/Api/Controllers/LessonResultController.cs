
using SmartGrader.Application.Dtos;
using SmartGrader.Application.Dtos.LessonResults;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Application.UseCases.LessonResults.ExportGradesPeriodReport;
using SmartGrader.Application.UseCases.LessonResults.ExportLessonResults;
using SmartGrader.Application.UseCases.LessonResults.GetLessonResult;
using SmartGrader.Application.UseCases.LessonResults.GetStudentGradesSummary;

namespace SmartGrader.Api.Controllers;

[ApiController]
[Route("api/lesson-results")]
[Authorize]
public class LessonResultController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILessonRepository _lessonRepository;
    public LessonResultController(
       IMediator mediator, IMapper mapper, ILessonRepository lessonRepository
        )
    {
        _mediator = mediator;
        _mapper = mapper;
        _lessonRepository = lessonRepository;

    }

    // תלמידה — רק לעצמה; מורה/מנהל — רק על שיעור בבעלותם (LessonAccess). באג קיים שתוקן: לפני התיקון
    // כל תלמידה מחוברת יכלה לקרוא תוצאות של כל תלמידה אחרת דרך ה-endpoint הזה.
    [HttpGet("{studentId:int}/{lessonId:int}")]
    public async Task<IActionResult> Get(int studentId, int lessonId, CancellationToken ct)
    {
        if (!IsAllowedForStudent(studentId))
            return Forbid();

        if (User.IsInRole("Teacher") || User.IsInRole("Admin"))
            await LessonAccess.GetOwnedOrThrowAsync(_lessonRepository, lessonId, OwnerScopeTeacherId, ct);

        var result = await _mediator.Send(new GetLessonResultQuery(studentId, lessonId), ct);
        return Ok(result);
    }

    [HttpGet("lesson/{lessonId:int}/export")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Export(int lessonId, CancellationToken ct)
    {
        byte[] bytes = await _mediator.Send(new ExportLessonResultsQuery(lessonId, OwnerScopeTeacherId), ct);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"lesson-{lessonId}-results.xlsx");
    }

    [HttpPost("complete")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Complete([FromBody] CompleteLessonRequestDto dto)
    {
        // ⚠️ לא ממופה דרך AutoMapper בכוונה — CompleteLessonCommand הוא record עם פרמטרים positional
        // חובה, שלא ממופה נכון מ-DTO דרך reflection. בנייה מפורשת.
        var command = new CompleteLessonCommand(dto.StudentId, dto.LessonId, dto.FinalScore, dto.HasBonus);
        var result = await _mediator.Send(command);
        var response = _mapper.Map<LessonResultResponseDto>(result);

        return Ok(response);
    }

    [HttpGet("export-report")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> ExportPeriodReport(
        [FromQuery] int fromYear, [FromQuery] int fromMonth, [FromQuery] int fromDay,
        [FromQuery] int toYear, [FromQuery] int toMonth, [FromQuery] int toDay,
        CancellationToken ct)
    {
        byte[] bytes = await _mediator.Send(
            new ExportGradesPeriodReportQuery(fromYear, fromMonth, fromDay, toYear, toMonth, toDay, OwnerScopeTeacherId), ct);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "grades-report.xlsx");
    }

    [HttpGet("student/{studentId:int}/summary")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetStudentSummary(int studentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStudentGradesSummaryQuery(studentId, OwnerScopeTeacherId), ct);
        return Ok(result);
    }
}

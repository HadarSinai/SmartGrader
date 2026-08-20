
using SmartGrader.Application.Dtos;
using SmartGrader.Application.Dtos.LessonResults;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Application.UseCases.LessonResults.ExportGradesPeriodReport;
using SmartGrader.Application.UseCases.LessonResults.ExportLessonResults;
using SmartGrader.Application.UseCases.LessonResults.GetLessonResult;
using SmartGrader.Application.UseCases.LessonResults.GetLessonScoreSuggestion;
using SmartGrader.Application.UseCases.LessonResults.GetStudentGradesSummary;
using SmartGrader.Application.UseCases.LessonResults.ReopenLesson;

namespace SmartGrader.Api.Controllers;

[ApiController]
[Route("api/lesson-results")]
[Authorize]
public class LessonResultController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public LessonResultController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    // תלמידה — רק לעצמה; מורה/מנהל — רק על שיעור בבעלותם (LessonAccess, בתוך ה-handler). באג קיים
    // שתוקן: לפני התיקון כל תלמידה מחוברת יכלה לקרוא תוצאות של כל תלמידה אחרת דרך ה-endpoint הזה.
    [HttpGet("{studentId:int}/{lessonId:int}")]
    public async Task<IActionResult> Get(int studentId, int lessonId, CancellationToken ct)
    {
        if (!IsAllowedForStudent(studentId))
            return Forbid();

        var result = await _mediator.Send(
            new GetLessonResultQuery(studentId, lessonId, TeacherIdForSharedRead), ct);
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

    // ההצעה לציון הסופי: מה כל תרגיל קיבל, וממוצע מוכן לעריכה.
    // 🔴 בלי זה המורה מחשבת ממוצע ביד לכל תלמידה בזמן שכל המספרים כבר במערכת.
    [HttpGet("{studentId:int}/{lessonId:int}/suggestion")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetScoreSuggestion(int studentId, int lessonId, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetLessonScoreSuggestionQuery(studentId, lessonId, OwnerScopeTeacherId), ct);
        return Ok(result);
    }

    // פתיחה מחדש של ציון סופי. ⚠️ עד כה CompleteWith זרק "Already completed" וציון סופי
    // שגוי לא היה ניתן לתיקון בשום דרך. הפתיחה גם משחררת את ההגשות של אותה תלמידה בשיעור.
    [HttpPost("{studentId:int}/{lessonId:int}/reopen")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Reopen(int studentId, int lessonId, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ReopenLessonCommand(studentId, lessonId, OwnerScopeTeacherId), ct);
        return Ok(_mapper.Map<LessonResultResponseDto>(result));
    }

    [HttpPost("complete")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Complete([FromBody] CompleteLessonRequestDto dto, CancellationToken ct)
    {
        // ⚠️ לא ממופה דרך AutoMapper בכוונה — CompleteLessonCommand הוא record עם פרמטרים positional
        // חובה, שלא ממופה נכון מ-DTO דרך reflection. בנייה מפורשת — וכאן בדיוק חסר OwnerScopeTeacherId
        // עד עכשיו, בזמן ש-Export ו-ExportPeriodReport באותו קונטרולר כן העבירו אותו.
        var command = new CompleteLessonCommand(
            dto.StudentId, dto.LessonId, dto.FinalScore, OwnerScopeTeacherId, dto.HasBonus);
        var result = await _mediator.Send(command, ct);
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

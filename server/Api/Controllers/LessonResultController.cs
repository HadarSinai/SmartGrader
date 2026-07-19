
using SmartGrader.Application.Dtos;
using SmartGrader.Application.Dtos.LessonResults;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Application.UseCases.LessonResults.ExportLessonResults;
using SmartGrader.Application.UseCases.LessonResults.GetLessonResult;

namespace SmartGrader.Api.Controllers;

[ApiController]
[Route("api/lesson-results")]
[Authorize]
public class LessonResultController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    public LessonResultController(
       IMediator mediator,IMapper mapper
        )
    {
        _mediator = mediator;
        _mapper = mapper;
        

    }

    [HttpGet("{studentId:int}/{lessonId:int}")]
    public async Task<IActionResult> Get(int studentId, int lessonId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLessonResultQuery(studentId, lessonId), ct);
        return Ok(result);
    }

    [HttpGet("lesson/{lessonId:int}/export")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Export(int lessonId, CancellationToken ct)
    {
        byte[] bytes = await _mediator.Send(new ExportLessonResultsQuery(lessonId), ct);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"lesson-{lessonId}-results.xlsx");
    }

    [HttpPost("complete")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Complete([FromBody] CompleteLessonRequestDto dto)
    {
        var command = _mapper.Map<CompleteLessonCommand>(dto);
        var result = await _mediator.Send(command);
        var response = _mapper.Map<LessonResultResponseDto>(result);

        return Ok(response);
    }
   
  
}


using SmartGrader.Application.Dtos;
using SmartGrader.Application.Dtos.LessonResults;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Application.UseCases.LessonResults.GetLessonResult;

namespace SmartGrader.Api.Controllers;

[ApiController]
[Route("api/lesson-results")]
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

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteLessonRequestDto dto)
    {
        var command = _mapper.Map<CompleteLessonCommand>(dto);
        var result = await _mediator.Send(command);
        var response = _mapper.Map<LessonResultResponseDto>(result);

        return Ok(response);
    }
   
  
}

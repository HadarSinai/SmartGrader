
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Api.Dtos;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;

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
//    | סוג                  | מתי?               | דוגמה                                    |
//| -------------------- | ------------------ | ---------------------------------------- |
//| **שמות עצם(Nouns)** | CRUD / משאב ברור   | `/api/students`, `/api/lessonresults/10` |
//| **פעלים(Verbs)**    | פעולה עסקית מיוחדת | `/api/lessonresults/complete`            |

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteLessonRequestDto dto)
    {
        var command = _mapper.Map<CompleteLessonCommand>(dto);
        var result = await _mediator.Send(command);
        var response = _mapper.Map<LessonResultResponseDto>(result);

        return Ok(response);
    }


}

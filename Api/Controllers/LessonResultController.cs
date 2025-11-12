using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;

namespace SmartGrader.Api.Controllers;

[ApiController]
[Route("api/lesson-results")]
public class LessonResultsController : ControllerBase
{
    private readonly CompleteLessonHandler _complete;
  

    public LessonResultsController(
        CompleteLessonHandler complete
        )
    {
        _complete = complete;
        
    }

    /// <summary>
    /// משלימה שיעור עם ציון סופי (יוצרת LessonResult אם לא קיים).
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteLessonCommand cmd, CancellationToken ct)
    {
        if (cmd.FinalScore is < 0 or > 100)
            return BadRequest("FinalScore must be between 0 and 100.");

        await _complete.Handle(cmd, ct);
        return NoContent();
    }

   
  
}

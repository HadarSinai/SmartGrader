using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.Classes;
using SmartGrader.Application.UseCases.Classes.CreateClass;
using SmartGrader.Application.UseCases.Classes.DeleteClass;
using SmartGrader.Application.UseCases.Classes.FinishYear;
using SmartGrader.Application.UseCases.Classes.GetClassById;
using SmartGrader.Application.UseCases.Classes.GetClasses;
using SmartGrader.Application.UseCases.Classes.UpdateClass;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/classes")]
    [Authorize(Roles = "Teacher,Admin")]
    public class ClassesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ClassesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool includeArchived,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SchoolClassResponseDto> result =
                await _mediator.Send(new GetClassesQuery(includeArchived), cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            SchoolClassResponseDto result =
                await _mediator.Send(new GetClassByIdQuery(id), cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateClassRequestDto dto,
            CancellationToken cancellationToken)
        {
            SchoolClassResponseDto created =
                await _mediator.Send(new CreateClassCommand(dto), cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateClassRequestDto dto,
            CancellationToken cancellationToken)
        {
            SchoolClassResponseDto updated =
                await _mediator.Send(new UpdateClassCommand(id, dto), cancellationToken);

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteClassCommand(id), cancellationToken);
            return NoContent();
        }

        // POST: api/classes/finish-year — מארכב את כל הכיתות הפעילות ("סיום שנה")
        //
        // ⚠️ מנהלת בלבד, ובכוונה יותר מחמיר משאר הבקר. הפעולה מארכבת בשאילתה אחת את *כל*
        // הכיתות הפעילות במוסד, לא רק את אלה של הקוראת — כל התלמידות הופכות לקריאה־בלבד
        // וכל ההגשות ננעלות (SubmissionLock נועל על כיתה מאורכבת). זה נכתב ישירות ל-DB
        // דרך ExecuteUpdate, בלי undo. לחיצה אחת של מורה אחת הקפיאה את כל בית הספר.
        [HttpPost("finish-year")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FinishYear(CancellationToken cancellationToken)
        {
            int archivedCount = await _mediator.Send(new FinishYearCommand(), cancellationToken);
            return Ok(new { archivedCount });
        }
    }
}

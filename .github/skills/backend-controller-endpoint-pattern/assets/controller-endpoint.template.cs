using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.{Entity};
using SmartGrader.Application.UseCases.{Entity}.Get{Entity}ById;
using SmartGrader.Application.UseCases.{Entity}.Create{Entity};
using SmartGrader.Application.UseCases.{Entity}.Update{Entity};
using SmartGrader.Application.UseCases.{Entity}.Delete{Entity};

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class {Entity}sController : ControllerBase
    {
        private readonly IMediator _mediator;

        public {Entity}sController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ---------- Flat resource variant ----------

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            {Entity}ResponseDto result = await _mediator.Send(new Get{Entity}ByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] Create{Entity}RequestDto dto,
            CancellationToken cancellationToken)
        {
            {Entity}ResponseDto created = await _mediator.Send(new Create{Entity}Command(dto), cancellationToken);

            // id MUST come from the response DTO returned by the handler, never the request DTO
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] Update{Entity}RequestDto dto,
            CancellationToken cancellationToken)
        {
            {Entity}ResponseDto updated = await _mediator.Send(new Update{Entity}Command(id, dto), cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new Delete{Entity}Command(id), cancellationToken);
            return NoContent();
        }

        // ---------- Nested-resource variant (e.g. children under a parent) ----------

        [HttpGet("{parentId:int}/children/{childId:int}")]
        public async Task<IActionResult> GetChildById(
            int parentId, int childId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetChildByIdQuery(parentId, childId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("{parentId:int}/children")]
        public async Task<IActionResult> CreateChild(
            int parentId,
            [FromBody] CreateChildRequestDto dto,
            CancellationToken cancellationToken)
        {
            var created = await _mediator.Send(new CreateChildCommand(parentId, dto), cancellationToken);

            return CreatedAtAction(
                nameof(GetChildById),
                new { parentId = parentId, childId = created.Id },
                created);
        }
    }
}

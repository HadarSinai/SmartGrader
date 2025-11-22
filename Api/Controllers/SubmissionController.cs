using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Api.Dtos.Submissions;
using SmartGrader.Application.UseCases.Submissions.CreateSubmission;
using SmartGrader.Application.UseCases.Submissions.DeleteSubmission;
using SmartGrader.Application.UseCases.Submissions.GetSubmissionById;
using SmartGrader.Application.UseCases.Submissions.GetSubmissions;
using SmartGrader.Application.UseCases.Submissions.UpdateSubmission;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public SubmissionsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var submissions = await _mediator.Send(new GetSubmissionsQuery(), cancellationToken);
            var response = _mapper.Map<IEnumerable<SubmissionResponseDto>>(submissions);
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var submission = await _mediator.Send(new GetSubmissionByIdQuery(id), cancellationToken);
            var response = _mapper.Map<SubmissionResponseDto>(submission);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubmissionRequestDto dto,
            CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateSubmissionCommand>(dto);
            var submission = await _mediator.Send(command, cancellationToken);
            var response = _mapper.Map<SubmissionResponseDto>(submission);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSubmissionRequestDto dto,
            CancellationToken cancellationToken)
        {
            var command = new UpdateSubmissionCommand(
                id,
                dto.StudentId,
                dto.AssignmentId,
                dto.SourceCode,
                dto.Score,
                dto.Comments
            );
            var updatedSubmission = await _mediator.Send(command, cancellationToken);
            var response = _mapper.Map<SubmissionResponseDto>(updatedSubmission);
            return Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteSubmissionCommand(id), cancellationToken);
            return NoContent();
        }
    }
}

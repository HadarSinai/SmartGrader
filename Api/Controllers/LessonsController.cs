using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.UseCases.Lessons.GetLessons;
using SmartGrader.Application.UseCases.Lessons.GetLessonById;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Application.UseCases.Lessons.UpdateLesson;
using SmartGrader.Application.UseCases.Lessons.DeleteLesson;
using SmartGrader.Application.UseCases.Assignments.GetAssignments;
using SmartGrader.Application.UseCases.Assignments.GetAssignmentById;
using SmartGrader.Application.UseCases.Assignments.DeleteAssignment;
using SmartGrader.Application.UseCases.Assignments.CreateAssignment;
using SmartGrader.Application.UseCases.Assignments.UpdateAssignment;


namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LessonsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            IReadOnlyList<LessonResponseDto> result =
                await _mediator.Send(new GetLessonsQuery(), cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            LessonResponseDto lesson =
                await _mediator.Send(new GetLessonByIdQuery(id), cancellationToken);

            return Ok(lesson);
        }


        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLessonRequestDto dto,
            CancellationToken cancellationToken)
        {
            LessonResponseDto created =
                await _mediator.Send(new CreateLessonCommand(dto), cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateLessonRequestDto dto,
            CancellationToken cancellationToken)
        {
            LessonResponseDto updated =
                await _mediator.Send(new UpdateLessonCommand(id, dto), cancellationToken);

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteLessonCommand(id), cancellationToken);
            return NoContent();
        }
        //--------------------------------------------------------------

        // GET: api/lessons/{lessonId}/assignments
        [HttpGet("{lessonId:int}/assignments")]
        public async Task<IActionResult> GetAssignments(
            int lessonId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<AssignmentResponseDto> result =
                await _mediator.Send(new GetAssignmentsQuery(lessonId), cancellationToken);

            return Ok(result);
        }

        // GET: api/lessons/{lessonId}/assignments/{assignmentId}
        [HttpGet("{lessonId:int}/assignments/{assignmentId:int}")]
        public async Task<IActionResult> GetAssignmentById(
            int lessonId,
            int assignmentId,
            CancellationToken cancellationToken)
        {
            AssignmentResponseDto result =
                await _mediator.Send(
                    new GetAssignmentByIdQuery(lessonId, assignmentId),
                    cancellationToken);

            return Ok(result);
        }

        [HttpPost("{lessonId:int}/assignments")]
        public async Task<IActionResult> CreateAssignment(
            int lessonId,
            [FromBody] CreateAssignmentRequestDto dto,
            CancellationToken cancellationToken)
        {
            AssignmentResponseDto created =
                await _mediator.Send(
                    new CreateAssignmentCommand(lessonId, dto),
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetAssignmentById),
                new { lessonId = lessonId, assignmentId = created.Id },
                created);
        }

        [HttpPut("{lessonId:int}/assignments/{assignmentId:int}")]
        public async Task<IActionResult> UpdateAssignment(
            int lessonId,
            int assignmentId,
            [FromBody] UpdateAssignmentRequestDto dto,
            CancellationToken cancellationToken)
        {
            AssignmentResponseDto updated =
                await _mediator.Send(
                    new UpdateAssignmentCommand(lessonId, assignmentId, dto),
                    cancellationToken);

            return Ok(updated);
        }

        [HttpDelete("{lessonId:int}/assignments/{assignmentId:int}")]
        public async Task<IActionResult> DeleteAssignment(
            int lessonId,
            int assignmentId,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new DeleteAssignmentCommand(lessonId, assignmentId),
                cancellationToken);

            return NoContent();
        }
    }
}

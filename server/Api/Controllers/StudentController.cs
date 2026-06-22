
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Application.UseCases.Students.CreateStudent;
using SmartGrader.Application.UseCases.Students.DeleteStudent;
using SmartGrader.Application.UseCases.Students.GetStudentById;
using SmartGrader.Application.UseCases.Students.GetStudents;
using SmartGrader.Application.UseCases.Students.UpdateStudent;
using SmartGrader.Application.UseCases.Submissions.CreateSubmission;
using SmartGrader.Application.UseCases.Submissions.DeleteSubmission;
using SmartGrader.Application.UseCases.Submissions.GetSubmissionById;
using SmartGrader.Application.UseCases.Submissions.GetSubmissions;
using SmartGrader.Application.UseCases.Submissions.UpdateSubmission;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StudentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

    

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            IReadOnlyList<StudentResponseDto> result =
                await _mediator.Send(new GetStudentsQuery(), cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            StudentResponseDto student =
                await _mediator.Send(new GetStudentByIdQuery(id), cancellationToken);

            return Ok(student);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateStudentRequestDto dto,
            CancellationToken cancellationToken)
        {
            StudentResponseDto created =
                await _mediator.Send(new CreateStudentCommand(dto), cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateStudentRequestDto dto,
            CancellationToken cancellationToken)
        {
            StudentResponseDto updated =
                await _mediator.Send(new UpdateStudentCommand(id, dto), cancellationToken);

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteStudentCommand(id), cancellationToken);
            return NoContent();
        }

     

        // GET: api/students/{studentId}/submissions
        [HttpGet("{studentId:int}/submissions")]
        public async Task<IActionResult> GetSubmissions(
            int studentId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SubmissionResponseDto> result =
                await _mediator.Send(new GetSubmissionsQuery(studentId), cancellationToken);

            return Ok(result);
        }

        // GET: api/students/{studentId}/submissions/{submissionId}
        [HttpGet("{studentId:int}/submissions/{submissionId:int}")]
        public async Task<IActionResult> GetSubmissionById(
            int studentId,
            int submissionId,
            CancellationToken cancellationToken)
        {
            SubmissionResponseDto result =
                await _mediator.Send(
                    new GetSubmissionByIdQuery(studentId, submissionId),
                    cancellationToken);

            return Ok(result);
        }

        // POST: api/students/{studentId}/submissions
        [HttpPost("{studentId:int}/submissions")]
        public async Task<IActionResult> CreateSubmission(
            int studentId,
            [FromBody] CreateSubmissionRequestDto dto,
            CancellationToken cancellationToken)
        {
            SubmissionResponseDto created =
                await _mediator.Send(
                    new CreateSubmissionCommand(studentId,  dto),
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetSubmissionById),
                new { studentId = studentId, submissionId = created.Id },
                created);
        }

        // PUT: api/students/{studentId}/submissions/{submissionId}
        [HttpPut("{studentId:int}/submissions/{submissionId:int}")]
        public async Task<IActionResult> UpdateSubmission(
            int studentId,
            int submissionId,
            [FromBody] UpdateSubmissionRequestDto dto,
            CancellationToken cancellationToken)
        {
            SubmissionResponseDto updated =
                await _mediator.Send(
                    new UpdateSubmissionCommand(studentId, submissionId, dto),
                    cancellationToken);

            return Ok(updated);
        }

        // DELETE: api/students/{studentId}/submissions/{submissionId}
        [HttpDelete("{studentId:int}/submissions/{submissionId:int}")]
        public async Task<IActionResult> DeleteSubmission(
            int studentId,
            int submissionId,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new DeleteSubmissionCommand(studentId,submissionId),
                cancellationToken);

            return NoContent();
        }
    }
}


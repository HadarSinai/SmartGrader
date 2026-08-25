using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.Teacher;
using SmartGrader.Application.UseCases.Teachers.CreateTeacher;
using SmartGrader.Application.UseCases.Teachers.DeleteTeacher;
using SmartGrader.Application.UseCases.Teachers.GetTeacherById;
using SmartGrader.Application.UseCases.Teachers.GetTeachers;
using SmartGrader.Application.UseCases.Teachers.ResetTeacherPassword;
using SmartGrader.Application.UseCases.Teachers.UpdateTeacher;

namespace SmartGrader.Api.Controllers
{
    /// <summary>
    /// ניהול חשבונות מורות בידי המנהלת. מרחיב <see cref="ControllerBase"/> ולא
    /// <c>ApiControllerBase</c>: אין כאן צמצום-בעלות לפי מורה — המנהלת רואה את כולן.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class TeachersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TeachersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>מזהה המשתמשת המחוברת — נדרש רק לחסימת מחיקה עצמית.</summary>
        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0");

        // GET: api/teachers
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            IReadOnlyList<TeacherResponseDto> result =
                await _mediator.Send(new GetTeachersQuery(), cancellationToken);

            return Ok(result);
        }

        // GET: api/teachers/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            TeacherResponseDto teacher = await _mediator.Send(new GetTeacherByIdQuery(id), cancellationToken);
            return Ok(teacher);
        }

        // POST: api/teachers
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateTeacherRequestDto dto,
            CancellationToken cancellationToken)
        {
            TeacherResponseDto created = await _mediator.Send(new CreateTeacherCommand(dto), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/teachers/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateTeacherRequestDto dto,
            CancellationToken cancellationToken)
        {
            TeacherResponseDto updated = await _mediator.Send(new UpdateTeacherCommand(id, dto), cancellationToken);
            return Ok(updated);
        }

        // POST: api/teachers/{id}/password — the admin resets a teacher's password
        [HttpPost("{id:int}/password")]
        public async Task<IActionResult> ResetPassword(
            int id,
            [FromBody] ResetTeacherPasswordRequestDto dto,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(new ResetTeacherPasswordCommand(id, dto), cancellationToken);
            return NoContent();
        }

        // DELETE: api/teachers/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteTeacherCommand(id, CurrentUserId), cancellationToken);
            return NoContent();
        }
    }
}

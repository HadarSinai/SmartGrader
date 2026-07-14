using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Application.UseCases.Auth.CreateAccountForStudent;
using SmartGrader.Application.UseCases.Auth.CreateStudentAccount;
using SmartGrader.Application.UseCases.Auth.Login;
using SmartGrader.Application.UseCases.Auth.RegisterTeacher;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto dto,
            CancellationToken cancellationToken)
        {
            AuthResponseDto result = await _mediator.Send(new LoginCommand(dto), cancellationToken);
            return Ok(result);
        }

        // POST: api/auth/register-teacher
        [HttpPost("register-teacher")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RegisterTeacher(
            [FromBody] RegisterTeacherRequestDto dto,
            CancellationToken cancellationToken)
        {
            AuthResponseDto result = await _mediator.Send(new RegisterTeacherCommand(dto), cancellationToken);
            return Ok(result);
        }

        // POST: api/auth/students — teacher creates a student account (User + Student)
        [HttpPost("students")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateStudentAccount(
            [FromBody] CreateStudentAccountRequestDto dto,
            CancellationToken cancellationToken)
        {
            StudentResponseDto created =
                await _mediator.Send(new CreateStudentAccountCommand(dto), cancellationToken);

            return CreatedAtAction(
                nameof(StudentsController.GetById),
                "Students",
                new { id = created.Id },
                created);
        }

        // POST: api/auth/students/{studentId}/account — create a login account for an existing student
        [HttpPost("students/{studentId:int}/account")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateAccountForStudent(
            int studentId,
            [FromBody] CreateAccountForStudentRequestDto dto,
            CancellationToken cancellationToken)
        {
            StudentResponseDto updated =
                await _mediator.Send(new CreateAccountForStudentCommand(studentId, dto), cancellationToken);

            return Ok(updated);
        }

        // GET: api/auth/me — current user info from token claims (no DB hit)
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "0");

            var studentIdClaim = User.FindFirstValue("studentId");

            var me = new CurrentUserDto(
                userId,
                User.FindFirstValue(ClaimTypes.Name) ?? "",
                User.FindFirstValue(ClaimTypes.Role) ?? "",
                studentIdClaim is null ? null : int.Parse(studentIdClaim));

            return Ok(me);
        }
    }
}

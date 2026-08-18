using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Teacher,Admin,Student")]
    public class NotificationsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("graded-submissions")]
        public async Task<IActionResult> GetGraded(
            [FromQuery] int limit = 20,
            CancellationToken cancellationToken = default)
        {
            int? teacherId = null;
            int? studentId = null;

            if (User.IsInRole("Student"))
            {
                var claim = User.FindFirstValue("studentId");
                if (claim is null || !int.TryParse(claim, out var ownStudentId))
                    return Forbid();
                studentId = ownStudentId;
            }
            else
            {
                teacherId = OwnerScopeTeacherId;
            }

            var result = await _mediator.Send(
                new GetRecentGradedSubmissionsQuery(teacherId, studentId, limit),
                cancellationToken);
            return Ok(result);
        }
    }
}

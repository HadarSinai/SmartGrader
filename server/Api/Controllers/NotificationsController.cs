using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Teacher,Admin")]
    public class NotificationsController : ControllerBase
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
            var result = await _mediator.Send(
                new GetRecentGradedSubmissionsQuery(limit),
                cancellationToken);
            return Ok(result);
        }
    }
}

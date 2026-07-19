using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.Logs;
using SmartGrader.Application.UseCases.Logs.DeleteOldLogs;
using SmartGrader.Application.UseCases.Logs.GetLogs;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/logs — latest 500 system logs, newest first
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            IReadOnlyList<LogResponseDto> result =
                await _mediator.Send(new GetLogsQuery(), cancellationToken);

            return Ok(result);
        }

        // DELETE: api/logs/old?days=30 — delete logs older than {days} days
        [HttpDelete("old")]
        public async Task<IActionResult> DeleteOld(
            [FromQuery] int days = 30,
            CancellationToken cancellationToken = default)
        {
            int deleted = await _mediator.Send(new DeleteOldLogsCommand(days), cancellationToken);
            return Ok(new { deleted });
        }
    }
}

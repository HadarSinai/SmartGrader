using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Services.Notifications;
using SmartGrader.Application.UseCases.Notifications.GetClassSignals;
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

        /// <summary>
        /// הסיגנלים על הכיתה ועל התרגילים מאתמול — מה שהפעמון של המורה מציג.
        /// <para>
        /// ⚠️ <b>Teacher,Admin בלבד.</b> הסיגנל אומר "8 מתוך 12 נכשלו בדרישה X"; לתלמידה
        /// אין בו שום ערך והיא לא אמורה לדעת את זה. הפעמון שלה נשאר על graded-submissions
        /// למעלה, ולכן אין כאן מסלול תלמידה כלל — לא לצמצם, לא להסתיר בלקוח.
        /// </para>
        /// <para>
        /// ⚠️ החלון הוא אותו חלון בדיוק שהדיג'סט היומי שולח (<c>ClassSignalPeriod.PreviousDay</c>),
        /// ולא פרמטר מהקורא: הפעמון והמייל חייבים להראות את אותו דבר, אחרת הם נקראים כשתי
        /// מערכות התראה שסותרות זו את זו.
        /// </para>
        /// </summary>
        [HttpGet("class-signals")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> GetClassSignals(CancellationToken cancellationToken = default)
        {
            var (fromUtc, toUtc) = ClassSignalPeriod.PreviousDay(DateTime.UtcNow);

            var result = await _mediator.Send(
                new GetClassSignalsQuery(OwnerScopeTeacherId, fromUtc, toUtc),
                cancellationToken);

            return Ok(result);
        }
    }
}

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Application.UseCases.Auth.ChangeMyPassword;
using SmartGrader.Application.UseCases.Auth.CreateAccountForStudent;
using SmartGrader.Application.UseCases.Auth.CreateStudentAccount;
using SmartGrader.Application.UseCases.Auth.ForgotPassword;
using SmartGrader.Application.UseCases.Auth.GetMyProfile;
using SmartGrader.Application.UseCases.Auth.Login;
using SmartGrader.Application.UseCases.Auth.ResetPassword;
using SmartGrader.Application.UseCases.Auth.UpdateMyProfile;

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

        /// <summary>
        /// מזהה המשתמשת המחוברת, מה-claims של הטוקן.
        /// <para>
        /// ⚠️ זהו המקור **היחיד** למזהה בנקודות ה-<c>me</c>. מזהה שמגיע מגוף הבקשה או מהנתיב
        /// היה הופך אותן לעריכה של חשבון כלשהו בידי כל מי שמחוברת.
        /// </para>
        /// </summary>
        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0");

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

        // POST: api/auth/forgot-password — anonymous request for a reset link
        //
        // 🔴 EnableRateLimiting אינו אופציונלי כאן, בשונה משאר הנקודות: זו הנקודה היחידה
        // במערכת שגורמת לשליחת מייל ליעד שאנונימית בוחרת. בלעדיו היא ממסרת דואר פתוחה,
        // ומי שמריצה עליה כתובת אחת בלולאה מציפה את תיבת המורה ושורפת את מכסת ה-SMTP.
        //
        // מחזירה 200 ריק תמיד — גם כשאין חשבון כזה. ר' ההסבר ב-ForgotPasswordHandler.
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequestDto dto,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(new ForgotPasswordCommand(dto), cancellationToken);
            return Ok();
        }

        // POST: api/auth/reset-password — set a new password using a one-time link
        //
        // מוגבלת בקצב מאותה סיבה: בלי זה אפשר לנחש טוקנים בקצב חופשי.
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequestDto dto,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(new ResetPasswordCommand(dto), cancellationToken);
            return Ok();
        }

        // ⚠️ אין כאן register-teacher. הרשמה עצמית נסגרה במכוון: הנקודה הייתה
        // [AllowAnonymous], כך שכל מי שהגיע לכתובת יצר לעצמו חשבון מורה מלא בלי שאף אחת
        // אישרה. חשבון מורה נוצר היום רק בידי המנהלת דרך POST /api/teachers.

        // POST: api/auth/students — teacher creates a student account (User + Student)
        [HttpPost("students")]
        [Authorize(Roles = "Teacher,Admin")]
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
        [Authorize(Roles = "Teacher,Admin")]
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
            var studentIdClaim = User.FindFirstValue("studentId");

            var me = new CurrentUserDto(
                CurrentUserId,
                User.FindFirstValue(ClaimTypes.Name) ?? "",
                User.FindFirstValue(ClaimTypes.Role) ?? "",
                studentIdClaim is null ? null : int.Parse(studentIdClaim));

            return Ok(me);
        }

        // GET: api/auth/me/profile — the signed-in user's account details, read from the DB
        //
        // ⚠️ נפרדת מ-GET me ולא מוזגה לתוכה: זו קוראת למסד, וזו שמעליה מסומנת במפורש
        // כחסרת-מסד ומשרתת כל בקשה שצריכה רק את ה-claims. המייל אינו claim בכוונה — טוקן
        // נשמר ב-localStorage וקריא לכל מי שמגיעה אליו.
        //
        // פתוחה לכל התפקידים: תלמידה רואה כאן את שם המשתמש והשם שלה לקריאה בלבד, לצד
        // החלפת הסיסמה. Email שלה יהיה null, וזה מצב תקין ולא שגיאה.
        [HttpGet("me/profile")]
        [Authorize]
        public async Task<IActionResult> MyProfile(CancellationToken cancellationToken)
        {
            MyProfileResponseDto result =
                await _mediator.Send(new GetMyProfileQuery(CurrentUserId), cancellationToken);

            return Ok(result);
        }

        // PUT: api/auth/me — the signed-in user changes her own name and email
        //
        // ⚠️ תלמידה מקבלת 403 כאן, וזו לא הגבלה שרירותית: השם שלה יושב גם על רשומת
        // ה-Student, ושינוי צד אחד היה מפצל בין מה שהיא רואה למה שהמורה שלה רואה. מייל
        // אין לה כלל.
        //
        // מחזירה AuthResponseDto ולא DTO של פרופיל — השם המלא הוא claim בתוך הטוקן, והטוקן
        // המרוענן הוא מה שמונע מהשם הישן להמשיך לחזור מ-GET me עד הכניסה הבאה.
        [HttpPut("me")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateMyProfileRequestDto dto,
            CancellationToken cancellationToken)
        {
            AuthResponseDto result =
                await _mediator.Send(new UpdateMyProfileCommand(CurrentUserId, dto), cancellationToken);

            return Ok(result);
        }

        // POST: api/auth/me/password — the signed-in user changes her own password
        //
        // פתוח לכל התפקידים: זו הפעולה היחידה שיש לתלמידה באזור האישי.
        //
        // ⚠️ ה-handler מאמת את הסיסמה הנוכחית לפני ההחלפה. [Authorize] מוודא רק שיש טוקן
        // תקף, ובלי האימות הזה מחשב שנשאר מחובר הוא השתלטות על החשבון בשתי לחיצות.
        [HttpPost("me/password")]
        [Authorize]
        public async Task<IActionResult> ChangeMyPassword(
            [FromBody] ChangeMyPasswordRequestDto dto,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(new ChangeMyPasswordCommand(CurrentUserId, dto), cancellationToken);
            return NoContent();
        }
    }
}

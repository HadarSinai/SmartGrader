
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Application.UseCases.Students.CreateStudent;
using SmartGrader.Application.UseCases.Students.DeleteStudent;
using SmartGrader.Application.UseCases.Students.ExportStudents;
using SmartGrader.Application.UseCases.Students.GetStudentById;
using SmartGrader.Application.UseCases.Students.GetStudents;
using SmartGrader.Application.UseCases.Students.ImportStudents;
using SmartGrader.Application.UseCases.Students.UpdateStudent;
using SmartGrader.Application.UseCases.Submissions.CreateSubmission;
using SmartGrader.Application.UseCases.Submissions.DeleteSubmission;
using SmartGrader.Application.UseCases.Submissions.GetSubmissionById;
using SmartGrader.Application.UseCases.Submissions.GetSubmissions;
using SmartGrader.Application.UseCases.Submissions.GrantExtraAttempt;
using SmartGrader.Application.UseCases.Submissions.OverrideScore;
using SmartGrader.Application.UseCases.Submissions.UpdateSubmission;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public StudentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // IsAllowedForStudent / TeacherIdForSharedRead / OwnerScopeTeacherId / IsPrivilegedUser
        // יושבים ב-ApiControllerBase.
        // ⚠️ IsAllowedForStudent לבדה מחזירה true לכל מורה — היא בודקת רק ש"תלמידה ניגשת לעצמה".
        // הסינון בין מורות נעשה במורד הזרם, לפי בעלות על השיעור, דרך TeacherId שמועבר לכל שאילתת הגשות.
        // ⚠️ !IsPrivilegedUser הוא הדגל שקובע אם להסתיר תוצאות של מקרי בדיקה מוסתרים. אין להחליף
        // אותו ב-TeacherIdForSharedRead is null — הוא null גם עבור מנהלת, שאמורה לראות הכל.


        [HttpGet]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool includeArchived,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<StudentResponseDto> result =
                await _mediator.Send(new GetStudentsQuery(includeArchived), cancellationToken);

            return Ok(result);
        }

        [HttpGet("export")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            byte[] bytes = await _mediator.Send(new ExportStudentsQuery(), cancellationToken);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "students.xlsx");
        }

        [HttpPost("import")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            const long maxFileSize = 5 * 1024 * 1024; // ~5MB

            if (file is null || file.Length == 0)
                return BadRequest(new { message = "לא נבחר קובץ להעלאה" });

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "יש להעלות קובץ Excel בפורמט xlsx בלבד" });

            if (file.Length > maxFileSize)
                return BadRequest(new { message = "הקובץ גדול מדי (מקסימום 5MB)" });

            await using var stream = file.OpenReadStream();
            ImportStudentsResultDto result =
                await _mediator.Send(new ImportStudentsCommand(stream), cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            if (!IsAllowedForStudent(id))
                return Forbid();

            StudentResponseDto student =
                await _mediator.Send(new GetStudentByIdQuery(id), cancellationToken);

            return Ok(student);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
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
        [Authorize(Roles = "Teacher,Admin")]
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
        [Authorize(Roles = "Teacher,Admin")]
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
            if (!IsAllowedForStudent(studentId))
                return Forbid();

            IReadOnlyList<SubmissionResponseDto> result =
                await _mediator.Send(
                    new GetSubmissionsQuery(studentId, TeacherIdForSharedRead, !IsPrivilegedUser),
                    cancellationToken);

            return Ok(result);
        }

        // GET: api/students/{studentId}/submissions/{submissionId}
        [HttpGet("{studentId:int}/submissions/{submissionId:int}")]
        public async Task<IActionResult> GetSubmissionById(
            int studentId,
            int submissionId,
            CancellationToken cancellationToken)
        {
            if (!IsAllowedForStudent(studentId))
                return Forbid();

            SubmissionResponseDto result =
                await _mediator.Send(
                    new GetSubmissionByIdQuery(studentId, submissionId, TeacherIdForSharedRead, !IsPrivilegedUser),
                    cancellationToken);

            return Ok(result);
        }

        // POST: api/students/{studentId}/submissions
        // Exception to the Teacher-only write rule: a student may submit code — for herself only
        [HttpPost("{studentId:int}/submissions")]
        public async Task<IActionResult> CreateSubmission(
            int studentId,
            [FromBody] CreateSubmissionRequestDto dto,
            CancellationToken cancellationToken)
        {
            if (!IsAllowedForStudent(studentId))
                return Forbid();

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
        // Exception to the Teacher-only write rule: a student may edit & resubmit her own
        // failed submission (CompilationFailed / JudgeUnavailable / AiFailed) — for herself only
        [HttpPut("{studentId:int}/submissions/{submissionId:int}")]
        public async Task<IActionResult> UpdateSubmission(
            int studentId,
            int submissionId,
            [FromBody] UpdateSubmissionRequestDto dto,
            CancellationToken cancellationToken)
        {
            if (!IsAllowedForStudent(studentId))
                return Forbid();

            SubmissionResponseDto updated =
                await _mediator.Send(
                    new UpdateSubmissionCommand(studentId, submissionId, dto, TeacherIdForSharedRead),
                    cancellationToken);

            return Ok(updated);
        }

        // POST: api/students/{studentId}/submissions/{submissionId}/extra-attempt
        //
        // אישור המורה לתלמידה להגיש שוב, מעל סף הציון.
        // ⚠️ כפתור ולא קוד משותף: קוד עובר בין תלמידות ומנטרל את הכלל בשקט. ההגנה כאן היא
        // ההזדהות הקיימת — Teacher,Admin בלבד — ואין צורך בשכבת סיסמה נוספת. TeacherUserId
        // נלקח מה-claims ולא מגוף הבקשה, אחרת יומן הביקורת הוא דיווח עצמי.
        [HttpPost("{studentId:int}/submissions/{submissionId:int}/extra-attempt")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> GrantExtraAttempt(
            int studentId,
            int submissionId,
            [FromBody] GrantExtraAttemptRequestDto dto,
            CancellationToken cancellationToken)
        {
            SubmissionResponseDto updated =
                await _mediator.Send(
                    new GrantExtraAttemptCommand(
                        submissionId, OwnerScopeTeacherId, CurrentUserId, dto.Reason),
                    cancellationToken);

            return Ok(updated);
        }

        // PUT: api/students/{studentId}/submissions/{submissionId}/score
        //
        // דריסת ציון בידי המורה — רשת ביטחון, לא חלק מהמסלול הרגיל.
        [HttpPut("{studentId:int}/submissions/{submissionId:int}/score")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> OverrideSubmissionScore(
            int studentId,
            int submissionId,
            [FromBody] OverrideScoreRequestDto dto,
            CancellationToken cancellationToken)
        {
            SubmissionResponseDto updated =
                await _mediator.Send(
                    new OverrideScoreCommand(
                        submissionId, OwnerScopeTeacherId, CurrentUserId, dto.Score, dto.Reason),
                    cancellationToken);

            return Ok(updated);
        }

        // DELETE: api/students/{studentId}/submissions/{submissionId}
        [HttpDelete("{studentId:int}/submissions/{submissionId:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteSubmission(
            int studentId,
            int submissionId,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new DeleteSubmissionCommand(studentId, submissionId, OwnerScopeTeacherId),
                cancellationToken);

            return NoContent();
        }
    }
}


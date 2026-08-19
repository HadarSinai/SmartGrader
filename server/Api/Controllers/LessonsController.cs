using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class LessonsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public LessonsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        // ⚠️ תלמידה קוראת ל-endpoints המשותפים האלה (GetAll/GetById/Assignments) גם היא — לכן ה-TeacherId
        // המועבר להם חייב להיות null עבורה, לא OwnerScopeTeacherId (שהיה שווה ל-CurrentUserId שלה,
        // מזהה שאינו מורה בכלל, וממוטט כל שיעור ל-404). בדיוק בגלל זה חובה להעביר גם StudentId —
        // בלעדיו אף בדיקה לא רצה עבורה וכל שיעור בבית הספר נקרא.
        // TeacherIdForSharedRead / TryResolveSharedReadScope יושבים ב-ApiControllerBase.

        // 1️⃣ GET api/lessons — תלמידה מקבלת רק את שיעורי הכיתה שלה; מורה הכל + סינון אופציונלי
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? classId,
            CancellationToken cancellationToken)
        {
            if (!TryResolveSharedReadScope(out var teacherId, out var studentId))
                return Forbid();

            if (studentId.HasValue)
                classId = null; // תלמידה לא בוחרת כיתה — נגזר מה-claim בלבד

            IReadOnlyList<LessonResponseDto> result =
                await _mediator.Send(new GetLessonsQuery(teacherId, classId, studentId), cancellationToken);

            return Ok(result);
        }

        // 2️⃣ GET api/lessons/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            if (!TryResolveSharedReadScope(out var teacherId, out var studentId))
                return Forbid();

            LessonResponseDto lesson =
                await _mediator.Send(new GetLessonByIdQuery(id, teacherId, studentId), cancellationToken);

            return Ok(lesson);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create(
            [FromBody] CreateLessonRequestDto dto,
            CancellationToken cancellationToken)
        {
            LessonResponseDto created =
                await _mediator.Send(new CreateLessonCommand(dto, CurrentUserId), cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateLessonRequestDto dto,
            CancellationToken cancellationToken)
        {
            LessonResponseDto updated =
                await _mediator.Send(new UpdateLessonCommand(id, dto, OwnerScopeTeacherId), cancellationToken);

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteLessonCommand(id, OwnerScopeTeacherId), cancellationToken);
            return NoContent();
        }
        //--------------------------------------------------------------

        // GET: api/lessons/{lessonId}/assignments
        [HttpGet("{lessonId:int}/assignments")]
        public async Task<IActionResult> GetAssignments(
            int lessonId,
            CancellationToken cancellationToken)
        {
            if (!TryResolveSharedReadScope(out var teacherId, out var studentId))
                return Forbid();

            IReadOnlyList<AssignmentResponseDto> result =
                await _mediator.Send(new GetAssignmentsQuery(lessonId, teacherId, studentId), cancellationToken);

            return Ok(result);
        }

        // GET: api/lessons/{lessonId}/assignments/{assignmentId}
        [HttpGet("{lessonId:int}/assignments/{assignmentId:int}")]
        public async Task<IActionResult> GetAssignmentById(
            int lessonId,
            int assignmentId,
            CancellationToken cancellationToken)
        {
            if (!TryResolveSharedReadScope(out var teacherId, out var studentId))
                return Forbid();

            AssignmentResponseDto result =
                await _mediator.Send(
                    new GetAssignmentByIdQuery(lessonId, assignmentId, teacherId, studentId),
                    cancellationToken);

            return Ok(result);
        }

        [HttpPost("{lessonId:int}/assignments")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> CreateAssignment(
            int lessonId,
            [FromBody] CreateAssignmentRequestDto dto,
            CancellationToken cancellationToken)
        {
            AssignmentResponseDto created =
                await _mediator.Send(
                    new CreateAssignmentCommand(lessonId, dto, OwnerScopeTeacherId),
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetAssignmentById),
                new { lessonId = lessonId, assignmentId = created.Id },
                created);
        }

        [HttpPut("{lessonId:int}/assignments/{assignmentId:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> UpdateAssignment(
            int lessonId,
            int assignmentId,
            [FromBody] UpdateAssignmentRequestDto dto,
            CancellationToken cancellationToken)
        {
            AssignmentResponseDto updated =
                await _mediator.Send(
                    new UpdateAssignmentCommand(lessonId, assignmentId, dto, OwnerScopeTeacherId),
                    cancellationToken);

            return Ok(updated);
        }

        [HttpDelete("{lessonId:int}/assignments/{assignmentId:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteAssignment(
            int lessonId,
            int assignmentId,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new DeleteAssignmentCommand(lessonId, assignmentId, OwnerScopeTeacherId),
                cancellationToken);

            return NoContent();
        }
    }
}

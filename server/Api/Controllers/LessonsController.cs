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

        // תלמידה קוראת ל-endpoints המשותפים האלה (GetAll/GetById/Assignments) גם היא — לכן ה-TeacherId
        // המועבר להם חייב להיות null עבורה, לא OwnerScopeTeacherId (שהיה שווה ל-CurrentUserId שלה,
        // מזהה שאינו מורה בכלל, וממוטט כל שיעור ל-404).
        private int? TeacherIdForSharedRead =>
            (User.IsInRole("Teacher") || User.IsInRole("Admin")) ? OwnerScopeTeacherId : null;

        // 1️⃣ GET api/lessons — תלמידה מקבלת רק את שיעורי הכיתה שלה; מורה הכל + סינון אופציונלי
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? classId,
            CancellationToken cancellationToken)
        {
            int? studentId = null;

            if (!User.IsInRole("Teacher") && !User.IsInRole("Admin"))
            {
                var claim = User.FindFirst("studentId")?.Value;
                if (claim is null || !int.TryParse(claim, out var ownId))
                    return Forbid();

                studentId = ownId;
                classId = null; // תלמידה לא בוחרת כיתה — נגזר מה-claim בלבד
            }

            IReadOnlyList<LessonResponseDto> result =
                await _mediator.Send(new GetLessonsQuery(TeacherIdForSharedRead, classId, studentId), cancellationToken);

            return Ok(result);
        }

        // 2️⃣ GET api/lessons/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            LessonResponseDto lesson =
                await _mediator.Send(new GetLessonByIdQuery(id, TeacherIdForSharedRead), cancellationToken);

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
            IReadOnlyList<AssignmentResponseDto> result =
                await _mediator.Send(new GetAssignmentsQuery(lessonId, TeacherIdForSharedRead), cancellationToken);

            return Ok(result);
        }

        // GET: api/lessons/{lessonId}/assignments/{assignmentId}
        [HttpGet("{lessonId:int}/assignments/{assignmentId:int}")]
        public async Task<IActionResult> GetAssignmentById(
            int lessonId,
            int assignmentId,
            CancellationToken cancellationToken)
        {
            AssignmentResponseDto result =
                await _mediator.Send(
                    new GetAssignmentByIdQuery(lessonId, assignmentId, TeacherIdForSharedRead),
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

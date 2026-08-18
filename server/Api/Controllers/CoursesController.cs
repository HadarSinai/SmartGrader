using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Dtos.Courses;
using SmartGrader.Application.UseCases.Courses.CreateCourse;
using SmartGrader.Application.UseCases.Courses.DeleteCourse;
using SmartGrader.Application.UseCases.Courses.GetCourseById;
using SmartGrader.Application.UseCases.Courses.GetCourses;
using SmartGrader.Application.UseCases.Courses.UpdateCourse;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Teacher,Admin")]
    public class CoursesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public CoursesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            IReadOnlyList<CourseResponseDto> result =
                await _mediator.Send(new GetCoursesQuery(OwnerScopeTeacherId), cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            CourseResponseDto result =
                await _mediator.Send(new GetCourseByIdQuery(id, OwnerScopeTeacherId), cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCourseRequestDto dto,
            CancellationToken cancellationToken)
        {
            CourseResponseDto created =
                await _mediator.Send(new CreateCourseCommand(dto, CurrentUserId), cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCourseRequestDto dto,
            CancellationToken cancellationToken)
        {
            CourseResponseDto updated =
                await _mediator.Send(new UpdateCourseCommand(id, dto, OwnerScopeTeacherId), cancellationToken);

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteCourseCommand(id, OwnerScopeTeacherId), cancellationToken);
            return NoContent();
        }
    }
}

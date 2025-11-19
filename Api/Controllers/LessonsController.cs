using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Api.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Application.UseCases.Lessons.DeleteLesson;
using SmartGrader.Application.UseCases.Lessons.GetLessonById;
using SmartGrader.Application.UseCases.Lessons.GetLessons;
using SmartGrader.Application.UseCases.Lessons.UpdateLesson;


namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        public LessonsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var lessons = await _mediator.Send(new GetLessonsQuery(), cancellationToken);

            var response = _mapper.Map<IEnumerable<LessonResponseDto>>(lessons);

            return Ok(response);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var lesson = await _mediator.Send(new GetLessonByIdQuery(id), cancellationToken);
            var response = _mapper.Map<LessonResponseDto>(lesson);
            return Ok(response);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLessonRequestDto dto,
        CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateLessonCommand>(dto);
            var lesson = await _mediator.Send(command, cancellationToken);
            var response = _mapper.Map<LessonResponseDto>(lesson);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }


        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLessonRequestDto dto,
        CancellationToken cancellationToken)
        {
            var command = new UpdateLessonCommand(
            id,
            dto.Name,
            dto.Subject,
            dto.LessonDate,
            dto.TeacherName
            );
            var updatedLesson = await _mediator.Send(command, cancellationToken);
            var response = _mapper.Map<LessonResponseDto>(updatedLesson);
            return Ok(response);
        }


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteLessonCommand(id), cancellationToken);
            return NoContent();
        }
    }
}

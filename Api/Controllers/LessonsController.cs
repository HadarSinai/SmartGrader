using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Api.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.GetLessons;
using SmartGrader.Application.UseCases.Lessons.GetLessonById;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Application.UseCases.Lessons.UpdateLesson;
using SmartGrader.Application.UseCases.Lessons.DeleteLesson;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]  // => api/lessons
    public class LessonsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public LessonsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        // 1️⃣ GET api/lessons
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lessons = await _mediator.Send(new GetLessonsQuery());

            var response = _mapper.Map<IEnumerable<LessonResponseDto>>(lessons);

            return Ok(response);
        }

        // 2️⃣ GET api/lessons/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lesson = await _mediator.Send(new GetLessonByIdQuery(id));
            if (lesson == null)
                return NotFound();

            var response = _mapper.Map<LessonResponseDto>(lesson);
            return Ok(response);
        }

        //// 3️⃣ POST api/lessons
        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] CreateLessonRequestDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var command = _mapper.Map<CreateLessonCommand>(dto);
        //    var lesson = await _mediator.Send(command);
        //    var response = _mapper.Map<LessonResponseDto>(lesson);

        //    return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        //}
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLessonRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var command = new CreateLessonCommand(
                dto.Name,
                dto.Subject,
                dto.LessonDate,
                dto.TeacherName
            );

            var lesson = await _mediator.Send(command);

            var response = _mapper.Map<LessonResponseDto>(lesson);

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }


        // 4️⃣ PUT api/lessons/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLessonRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var command = new UpdateLessonCommand(
                id,
                dto.Name,
                dto.Subject,
                dto.LessonDate,
                dto.TeacherName
            );

            var updatedLesson = await _mediator.Send(command);
            var response = _mapper.Map<LessonResponseDto>(updatedLesson);

            return Ok(response);
            // אם תרצי בלי Response אפשר:
            // return NoContent();
        }

        // 5️⃣ DELETE api/lessons/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteLessonCommand(id));
            return NoContent();
        }
    }
}

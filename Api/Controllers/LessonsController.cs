using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Api.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using SmartGrader.Application.UseCases.Lessons.DeleteLesson;
using SmartGrader.Application.UseCases.Lessons.GetLessonById;
using SmartGrader.Application.UseCases.Lessons.GetLessons;
using SmartGrader.Application.UseCases.Lessons.UpdateLesson;
using System.Threading;

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

        // 1️⃣ GET api/lessons
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var lessons = await _mediator.Send(new GetLessonsQuery());

            var response = _mapper.Map<IEnumerable<LessonResponseDto>>(lessons);

            return Ok(response);
        }

        // 2️⃣ GET api/lessons/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var lesson = await _mediator.Send(new GetLessonByIdQuery(id));
            if (lesson == null)
                return NotFound();

            var response = _mapper.Map<LessonResponseDto>(lesson);
            return Ok(response);
        }

       
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLessonRequestDto dto,
            CancellationToken cancellationToken)
        {
     

            // מיפוי DTO → Command באופן אוטומטי
            var command = _mapper.Map<CreateLessonCommand>(dto);

            // שליחת הבקשה למערכת העסקית (Application Layer)
            var lesson = await _mediator.Send(command);

            // מיפוי Entity → Response DTO
            var response = _mapper.Map<LessonResponseDto>(lesson);

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
      int id,
      [FromBody] UpdateLessonRequestDto dto,
      CancellationToken cancellationToken)
        {

            // בניית ה־Command באופן מפורש וברור
            var command = new UpdateLessonCommand(
                id,
                dto.Name,
                dto.Subject,
                dto.LessonDate,
                dto.TeacherName
            );

            // שליחת הבקשה לשכבת ה־Application עם token
            var updatedLesson = await _mediator.Send(command, cancellationToken);

            // מיפוי ה־Entity ל־Response DTO
            var response = _mapper.Map<LessonResponseDto>(updatedLesson);

            return Ok(response);
        }



        // 5️⃣ DELETE api/lessons/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id,CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteLessonCommand(id));
            return NoContent();
        }
    }
}

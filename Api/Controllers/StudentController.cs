using Api.Dtos.Student;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.UseCases.Students.CreateStudent;
using SmartGrader.Application.UseCases.Students.DeleteStudent;
using SmartGrader.Application.UseCases.Students.GetStudentById;
using SmartGrader.Application.UseCases.Students.GetStudents;
using SmartGrader.Application.UseCases.Students.UpdateStudent;

namespace SmartGrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public StudentsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        // 1️⃣ GET api/students
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var students = await _mediator.Send(new GetStudentQuery());
            var response = _mapper.Map<IEnumerable<StudentResponseDto>>(students);
            return Ok(response);
        }

        // 2️⃣ GET api/students/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var student = await _mediator.Send(new GetStudentByIdQuery(id));
            if (student == null)
                return NotFound();

            var response = _mapper.Map<StudentResponseDto>(student);
            return Ok(response);
        }

        // 3️⃣ POST api/students
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentRequestDto dto,
            CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateStudentCommand>(dto);
            var student = await _mediator.Send(command);
            var response = _mapper.Map<StudentResponseDto>(student);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        // 4️⃣ PUT api/students/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id,
            [FromBody] UpdateStudentRequestDto dto,
            CancellationToken cancellationToken)
        {
            var command = new UpdateStudentCommand(
                id,
                dto.FullName,
                dto.ClassName
            );

            var updatedStudent = await _mediator.Send(command, cancellationToken);
            var response = _mapper.Map<StudentResponseDto>(updatedStudent);
            return Ok(response);
        }

        // 5️⃣ DELETE api/students/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteStudentCommand(id));
            return NoContent();
        }
    }
}

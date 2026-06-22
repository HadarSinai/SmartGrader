using MediatR;
using SmartGrader.Application.Dtos.Student;

namespace SmartGrader.Application.UseCases.Students.CreateStudent
{
    public record CreateStudentCommand(
        CreateStudentRequestDto Dto
    ) : IRequest<StudentResponseDto>;
}

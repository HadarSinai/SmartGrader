using MediatR;
using SmartGrader.Application.Dtos.Student;

namespace SmartGrader.Application.UseCases.Students.UpdateStudent
{
    public record UpdateStudentCommand(
        int Id,
        UpdateStudentRequestDto Dto
    ) : IRequest<StudentResponseDto>;
}

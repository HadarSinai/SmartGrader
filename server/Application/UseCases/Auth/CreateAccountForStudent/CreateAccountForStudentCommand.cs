using MediatR;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Application.Dtos.Student;

namespace SmartGrader.Application.UseCases.Auth.CreateAccountForStudent
{
    public record CreateAccountForStudentCommand(int StudentId, CreateAccountForStudentRequestDto Dto)
        : IRequest<StudentResponseDto>;
}

using MediatR;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Application.Dtos.Student;

namespace SmartGrader.Application.UseCases.Auth.CreateStudentAccount
{
    public record CreateStudentAccountCommand(CreateStudentAccountRequestDto Dto) : IRequest<StudentResponseDto>;
}

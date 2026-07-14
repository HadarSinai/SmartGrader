using MediatR;
using SmartGrader.Application.Dtos.Auth;

namespace SmartGrader.Application.UseCases.Auth.RegisterTeacher
{
    public record RegisterTeacherCommand(RegisterTeacherRequestDto Dto) : IRequest<AuthResponseDto>;
}

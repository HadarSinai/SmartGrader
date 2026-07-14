using MediatR;
using SmartGrader.Application.Dtos.Auth;

namespace SmartGrader.Application.UseCases.Auth.Login
{
    public record LoginCommand(LoginRequestDto Dto) : IRequest<AuthResponseDto>;
}

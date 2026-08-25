using MediatR;
using SmartGrader.Application.Dtos.Auth;

namespace SmartGrader.Application.UseCases.Auth.ResetPassword
{
    public record ResetPasswordCommand(ResetPasswordRequestDto Dto) : IRequest;
}

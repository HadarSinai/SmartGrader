using MediatR;
using SmartGrader.Application.Dtos.Teacher;

namespace SmartGrader.Application.UseCases.Teachers.ResetTeacherPassword
{
    public record ResetTeacherPasswordCommand(int Id, ResetTeacherPasswordRequestDto Dto) : IRequest;
}

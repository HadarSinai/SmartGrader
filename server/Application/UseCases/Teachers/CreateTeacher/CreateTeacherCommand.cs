using MediatR;
using SmartGrader.Application.Dtos.Teacher;

namespace SmartGrader.Application.UseCases.Teachers.CreateTeacher
{
    public record CreateTeacherCommand(CreateTeacherRequestDto Dto) : IRequest<TeacherResponseDto>;
}

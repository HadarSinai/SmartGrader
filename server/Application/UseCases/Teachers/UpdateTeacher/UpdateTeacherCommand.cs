using MediatR;
using SmartGrader.Application.Dtos.Teacher;

namespace SmartGrader.Application.UseCases.Teachers.UpdateTeacher
{
    public record UpdateTeacherCommand(int Id, UpdateTeacherRequestDto Dto) : IRequest<TeacherResponseDto>;
}

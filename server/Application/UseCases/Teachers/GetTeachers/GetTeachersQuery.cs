using MediatR;
using SmartGrader.Application.Dtos.Teacher;

namespace SmartGrader.Application.UseCases.Teachers.GetTeachers
{
    public record GetTeachersQuery() : IRequest<IReadOnlyList<TeacherResponseDto>>;
}

using MediatR;
using SmartGrader.Application.Dtos.Teacher;

namespace SmartGrader.Application.UseCases.Teachers.GetTeacherById
{
    public record GetTeacherByIdQuery(int Id) : IRequest<TeacherResponseDto>;
}

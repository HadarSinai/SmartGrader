using MediatR;
using SmartGrader.Application.Dtos.Classes;

namespace SmartGrader.Application.UseCases.Classes.GetClassById
{
    public record GetClassByIdQuery(int Id) : IRequest<SchoolClassResponseDto>;
}

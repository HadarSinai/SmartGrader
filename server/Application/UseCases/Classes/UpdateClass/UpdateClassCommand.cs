using MediatR;
using SmartGrader.Application.Dtos.Classes;

namespace SmartGrader.Application.UseCases.Classes.UpdateClass
{
    public record UpdateClassCommand(int Id, UpdateClassRequestDto Dto) : IRequest<SchoolClassResponseDto>;
}

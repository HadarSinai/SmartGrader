using MediatR;
using SmartGrader.Application.Dtos.Classes;

namespace SmartGrader.Application.UseCases.Classes.CreateClass
{
    public record CreateClassCommand(CreateClassRequestDto Dto) : IRequest<SchoolClassResponseDto>;
}

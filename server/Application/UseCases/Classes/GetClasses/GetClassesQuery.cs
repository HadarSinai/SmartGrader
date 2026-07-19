using MediatR;
using SmartGrader.Application.Dtos.Classes;

namespace SmartGrader.Application.UseCases.Classes.GetClasses
{
    public record GetClassesQuery(bool IncludeArchived = false) : IRequest<IReadOnlyList<SchoolClassResponseDto>>;
}

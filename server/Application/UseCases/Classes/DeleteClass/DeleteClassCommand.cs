using MediatR;

namespace SmartGrader.Application.UseCases.Classes.DeleteClass
{
    public record DeleteClassCommand(int Id) : IRequest;
}

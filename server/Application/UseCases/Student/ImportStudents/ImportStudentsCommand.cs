using MediatR;
using SmartGrader.Application.Dtos.Student;

namespace SmartGrader.Application.UseCases.Students.ImportStudents
{
    public record ImportStudentsCommand(Stream FileStream) : IRequest<ImportStudentsResultDto>;
}

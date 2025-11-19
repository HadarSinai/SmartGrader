using MediatR;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.UpdateStudent
{
    public record UpdateStudentCommand(
        int Id,
        string FullName,
        string ClassName
    ) : IRequest<Student>;
}

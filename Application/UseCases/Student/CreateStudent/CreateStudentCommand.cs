using MediatR;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.CreateStudent
{
    public record CreateStudentCommand(
        string FullName,
        string ClassName
    ) : IRequest<Student>;
}

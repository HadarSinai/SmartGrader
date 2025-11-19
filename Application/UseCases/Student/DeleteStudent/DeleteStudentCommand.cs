using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.DeleteStudent
{
    public record DeleteStudentCommand(int Id) : IRequest;
}

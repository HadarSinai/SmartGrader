using MediatR;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.GetStudents
{
    public record GetStudentsQuery() : IRequest<IReadOnlyList<StudentResponseDto>>;
}

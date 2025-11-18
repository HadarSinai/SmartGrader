//using Application.UseCases.Student.GetStudent;
using Domain.Abstractions;
using MediatR;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.GetStudents
{
    public class GetStudentHandler
        : IRequestHandler<GetStudentQuery, IReadOnlyList<Student>>
    {
        private readonly IStudentRepository _repository;

        public GetStudentHandler(IStudentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<Student>> Handle(
            GetStudentQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetAllAsync(cancellationToken);
        }
    }
}

//using Application.UseCases.Student.GetStudentById;
using Domain.Abstractions;
using MediatR;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.GetStudentById
{
    public class GetStudentByIdHandler
        : IRequestHandler<GetStudentByIdQuery, Student?>
    {
        private readonly IStudentRepository _repository;

        public GetStudentByIdHandler(IStudentRepository repository)
        {
            _repository = repository;
        }

        public async Task<Student?> Handle(
            GetStudentByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}

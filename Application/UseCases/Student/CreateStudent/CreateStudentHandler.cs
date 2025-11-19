using Domain.Abstractions;
using MediatR;
using SmartGrader.Application.UseCases.Students.CreateStudent;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.CreateStudent
{
    public class CreateStudentHandler
        : IRequestHandler<CreateStudentCommand, Student>
    {
        private readonly IStudentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateStudentHandler(
            IStudentRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Student> Handle(
            CreateStudentCommand request,
            CancellationToken cancellationToken)
        {
            var student = new Student
            {
                FullName = request.FullName,
                ClassName = request.ClassName
                // CreatedAt מוגדר אוטומטית ב-ctor / default
            };

            await _repository.AddAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return student;
        }
    }
}

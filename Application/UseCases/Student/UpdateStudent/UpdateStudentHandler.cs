using Domain.Abstractions;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.UpdateStudent
{
    public class UpdateStudentHandler
        : IRequestHandler<UpdateStudentCommand, Student>
    {
        private readonly IStudentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateStudentHandler(
            IStudentRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Student> Handle(
            UpdateStudentCommand request,
            CancellationToken cancellationToken)
        {
            var student = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (student is null)
            {
                // כאן התלמיד לא נמצא → זורקים NotFoundException
                throw new NotFoundException(nameof(Student), request.Id);
            }

            student.FullName = request.FullName;
            student.ClassName = request.ClassName;

            await _repository.UpdateAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return student;
        }
    }
}

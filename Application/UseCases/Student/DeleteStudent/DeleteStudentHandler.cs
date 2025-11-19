using Domain.Abstractions;
using MediatR;
using SmartGrader.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.DeleteStudent
{
    public class DeleteStudentHandler : IRequestHandler<DeleteStudentCommand>
    {
        private readonly IStudentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteStudentHandler(
            IStudentRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            DeleteStudentCommand request,
            CancellationToken cancellationToken)
        {
            var student = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (student == null)
                throw new KeyNotFoundException($"Student {request.Id} not found.");

            await _repository.DeleteAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // IRequest בלי טיפוס מחזיר Unit
            return Unit.Value;
        }

        Task IRequestHandler<DeleteStudentCommand>.Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
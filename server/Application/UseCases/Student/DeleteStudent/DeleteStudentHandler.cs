using Domain.Abstractions;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

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

        public async Task Handle(
            DeleteStudentCommand request,
            CancellationToken cancellationToken)
        {
            var student = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (student is null)
                throw new NotFoundException("Student", request.Id);

            await _repository.DeleteAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

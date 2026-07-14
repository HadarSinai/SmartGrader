
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Students.DeleteStudent
{
    public class DeleteStudentHandler : IRequestHandler<DeleteStudentCommand>
    {
        private readonly IStudentRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteStudentHandler(
            IStudentRepository repository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            DeleteStudentCommand request,
            CancellationToken cancellationToken)
        {
            var student = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (student is null)
                throw new NotFoundException("Student", request.Id);

            // Deleting a student also deletes her login account (User), if one exists
            if (student.UserId is int userId)
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user is not null)
                    await _userRepository.DeleteAsync(user, cancellationToken);
            }

            await _repository.DeleteAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.DeleteSubmission
{
    public class DeleteSubmissionHandler : IRequestHandler<DeleteSubmissionCommand, Unit>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteSubmissionHandler(ISubmissionRepository repository, IUnitOfWork unitOfWork)
            => (_repository, _unitOfWork) = (repository, unitOfWork);

        public async Task<Unit> Handle(DeleteSubmissionCommand request, CancellationToken cancellationToken)
        {
            var submission = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (submission is null)
                throw new NotFoundException(nameof(Submission), request.Id);

            await _repository.DeleteAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}

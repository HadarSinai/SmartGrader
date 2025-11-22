using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.UpdateSubmission
{
    public class UpdateSubmissionHandler
        : IRequestHandler<UpdateSubmissionCommand, Submission>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSubmissionHandler(ISubmissionRepository repository, IUnitOfWork unitOfWork)
            => (_repository, _unitOfWork) = (repository, unitOfWork);

        public async Task<Submission> Handle(
            UpdateSubmissionCommand request,
            CancellationToken cancellationToken)
        {
            var submission = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (submission is null)
            {
                throw new NotFoundException(nameof(Submission), request.Id);
            }

            submission.StudentId = request.StudentId;
            submission.AssignmentId = request.AssignmentId;
            submission.SourceCode = request.SourceCode;
            submission.Score = request.Score;
            submission.Comments = request.Comments;

            await _repository.UpdateAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return submission;
        }
    }
}

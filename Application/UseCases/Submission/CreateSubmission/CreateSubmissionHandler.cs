using MediatR;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.CreateSubmission
{
    public class CreateSubmissionHandler
        : IRequestHandler<CreateSubmissionCommand, Submission>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateSubmissionHandler(
            ISubmissionRepository repository,
            IUnitOfWork unitOfWork)
            => (_repository, _unitOfWork) = (repository, unitOfWork);

        public async Task<Submission> Handle(
            CreateSubmissionCommand request,
            CancellationToken cancellationToken)
        {
            //var submission = Submission.Create(
            //   request.StudentId,
            //   request.AssignmentId,
            //   request.SourceCode,
            //   request.Comments
            //);
            var submission = new Submission
            {
                StudentId = request.StudentId,
                AssignmentId = request.AssignmentId,
                SourceCode = request.SourceCode,
                Score = request.Score,
                Comments = request.Comments
            };

            await _repository.AddAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return submission;
        }
    }
}
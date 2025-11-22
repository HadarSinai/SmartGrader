using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissionById
{
    public class GetSubmissionByIdHandler
        : IRequestHandler<GetSubmissionByIdQuery, Submission>
    {
        private readonly ISubmissionRepository _repository;

        public GetSubmissionByIdHandler(ISubmissionRepository repository)
            => _repository = repository;

        public async Task<Submission> Handle(GetSubmissionByIdQuery request, CancellationToken ct)
        {
            var submission = await _repository.GetByIdAsync(request.Id, ct);

            if (submission is null)
                throw new NotFoundException(nameof(Submission), request.Id);

            return submission;
        }
    }
}

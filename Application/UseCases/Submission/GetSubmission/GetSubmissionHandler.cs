using MediatR;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissions
{
    public class GetSubmissionsHandler
        : IRequestHandler<GetSubmissionsQuery, IReadOnlyList<Submission>>
    {
        private readonly ISubmissionRepository _repository;

        public GetSubmissionsHandler(ISubmissionRepository repository)
            => _repository = repository;

        public async Task<IReadOnlyList<Submission>> Handle(
            GetSubmissionsQuery request,
            CancellationToken cancellationToken)
        {
            var submissions = await _repository.GetAllAsync(cancellationToken);
            return submissions;
        }
    }
}

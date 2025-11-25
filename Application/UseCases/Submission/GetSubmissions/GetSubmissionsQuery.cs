using MediatR;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissions
{
    public record GetSubmissionsQuery(int StudentId) : IRequest<IReadOnlyList<SubmissionResponseDto>>;
}

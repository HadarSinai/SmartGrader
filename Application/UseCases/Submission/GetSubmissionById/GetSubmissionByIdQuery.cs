using MediatR;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissionById
{
    public record GetSubmissionByIdQuery(int Id) : IRequest<Submission?>;
}

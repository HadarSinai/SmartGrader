using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.DeleteSubmission
{
    public record DeleteSubmissionCommand(int Id) : IRequest<Unit>;
}

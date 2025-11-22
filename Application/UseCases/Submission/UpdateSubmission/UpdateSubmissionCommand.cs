using MediatR;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.UpdateSubmission
{
    public record UpdateSubmissionCommand(
        int Id,
        int StudentId,
        int AssignmentId,
        string SourceCode,
        double Score,
        string Comments
    ) : IRequest<Submission>;
}

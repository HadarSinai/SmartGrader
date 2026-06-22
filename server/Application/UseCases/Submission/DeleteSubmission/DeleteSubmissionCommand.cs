using MediatR;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Submissions.DeleteSubmission
{
    public record DeleteSubmissionCommand(int StudentId,int SubmissionId) : IRequest;
}

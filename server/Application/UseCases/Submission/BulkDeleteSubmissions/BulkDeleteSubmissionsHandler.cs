using MediatR;
using SmartGrader.Application.Common.BulkDelete;
using SmartGrader.Application.Dtos.Common;
using SmartGrader.Application.UseCases.Submissions.DeleteSubmission;

namespace SmartGrader.Application.UseCases.Submissions.BulkDeleteSubmissions
{
    /// <summary>
    /// מוחקת את ההגשות שנבחרו, אחת-אחת, דרך <see cref="DeleteSubmissionCommand"/> עצמה —
    /// כולל סינון הבעלות, כולל האיסור למחוק הגשה שהבדיקה עליה פועלת, וכולל האיסור למחוק
    /// הגשה שכבר קיבלה ציון. ר' <see cref="BulkDeleteRunner"/>.
    /// <para>
    /// ⚠️ דווקא כאן הצלחה חלקית היא ברירת המחדל ולא החריג: מסך ההגשות מציג בעיקר הגשות
    /// שנבדקו, ובחירת "הכול" תסרב כמעט לכולן. זו התנהגות נכונה — הגשה שנבדקה נושאת ציון —
    /// ולכן הדיאלוג חייב להראות את הסיבות ולא רק מספר.
    /// </para>
    /// </summary>
    public class BulkDeleteSubmissionsHandler
        : IRequestHandler<BulkDeleteSubmissionsCommand, BulkDeleteResultDto>
    {
        private readonly IMediator _mediator;

        public BulkDeleteSubmissionsHandler(IMediator mediator) => _mediator = mediator;

        public Task<BulkDeleteResultDto> Handle(
            BulkDeleteSubmissionsCommand request,
            CancellationToken ct) =>
            BulkDeleteRunner.RunAsync(
                request.SubmissionIds,
                id => _mediator.Send(
                    new DeleteSubmissionCommand(request.StudentId, id, request.TeacherId), ct),
                ct);
    }
}

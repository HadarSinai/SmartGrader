using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

public class CompleteLessonHandler
    : IRequestHandler<CompleteLessonCommand, LessonResult>
{
    private readonly ILessonResultRepository _repository;
    private readonly ISubmissionRepository _submissions;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteLessonHandler(
        ILessonResultRepository repository,
        ISubmissionRepository submissions,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _submissions = submissions;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
    }

    public async Task<LessonResult> Handle(CompleteLessonCommand command, CancellationToken ct)
    {
        // כתיבת ציון סופי מותרת רק למורה שהשיעור בבעלותה (null = מנהל/ת). 404 ולא 403 — ר' LessonAccess.
        await LessonAccess.GetOwnedOrThrowAsync(_lessons, command.LessonId, command.TeacherId, ct);

        // אין לסכם שיעור כשההגשה האחרונה של התלמיד באחד התרגילים עדיין לא הגיעה למצב סופי.
        // AiFailed מותר בכוונה — מאפשר למורה לתת ציון ידני כשה-AI נכשל.
        var submissions = await _submissions.GetByStudentAndLessonAsync(
            command.StudentId, command.LessonId, ct);

        var blocking = submissions
            .GroupBy(s => s.AssignmentId)
            .Select(g => g.OrderByDescending(s => s.SubmittedAt).First())
            .FirstOrDefault(s => s.Status is SubmissionStatus.PendingAi
                or SubmissionStatus.ProcessingAi
                or SubmissionStatus.CompilationFailed
                or SubmissionStatus.JudgeUnavailable);

        if (blocking is not null)
        {
            var assignmentName = blocking.Assignment?.Title ?? $"#{blocking.AssignmentId}";
            var reason = blocking.Status switch
            {
                SubmissionStatus.PendingAi or SubmissionStatus.ProcessingAi =>
                    $"ההגשה לתרגיל \"{assignmentName}\" עדיין בבדיקה",
                SubmissionStatus.CompilationFailed =>
                    $"ההגשה לתרגיל \"{assignmentName}\" נכשלה בקומפילציה וממתינה להגשה מחדש",
                _ =>
                    $"ההגשה לתרגיל \"{assignmentName}\" נעצרה בגלל תקלה במערכת הבדיקה",
            };
            throw new BusinessRuleException($"לא ניתן לסכם את השיעור — {reason}");
        }

        var result = await _repository.GetAsync(command.StudentId, command.LessonId, ct)
                     ?? LessonResult.Create(command.StudentId, command.LessonId);

        result.CompleteWith(command.FinalScore, command.HasBonus);

        if (result.Id == 0)
            await _repository.AddAsync(result, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return result;
    }
}

using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.LessonResults.ReopenLesson;

public class ReopenLessonHandler : IRequestHandler<ReopenLessonCommand, LessonResult>
{
    private readonly ILessonResultRepository _repository;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;

    public ReopenLessonHandler(
        ILessonResultRepository repository,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
    }

    public async Task<LessonResult> Handle(ReopenLessonCommand request, CancellationToken ct)
    {
        // אותה בדיקת בעלות כמו בסיום שיעור. 404 ולא 403 — ר' LessonAccess.
        await LessonAccess.GetOwnedOrThrowAsync(_lessons, request.LessonId, request.TeacherId, ct);

        var result = await _repository.GetAsync(request.StudentId, request.LessonId, ct);

        if (result is null)
            throw new NotFoundException(nameof(LessonResult), request.LessonId);

        if (!result.IsComplete)
            throw new BusinessRuleException("השיעור אינו מסוכם לתלמידה זו, ואין מה לפתוח מחדש.");

        result.Reopen();

        await _unitOfWork.SaveChangesAsync(ct);

        return result;
    }
}

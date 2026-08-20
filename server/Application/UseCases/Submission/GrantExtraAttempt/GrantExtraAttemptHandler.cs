using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Submissions.GrantExtraAttempt;

/// <summary>
/// מאשר לתלמידה ניסיון נוסף. <b>גובר על סף הציון</b>, ומתועד.
/// <para>
/// כפתור ולא קוד משותף: קוד עובר בין תלמידות ומנטרל את הכלל בשקט. ההגנה כאן היא ההזדהות
/// הקיימת — <c>[Authorize(Roles = "Teacher,Admin")]</c> בבקר יחד עם סינון בעלות המורה
/// שב-<c>GetByIdAsync</c> — ואין צורך בשכבת סיסמה נוספת.
/// </para>
/// </summary>
public class GrantExtraAttemptHandler
    : IRequestHandler<GrantExtraAttemptCommand, SubmissionResponseDto>
{
    private readonly ISubmissionRepository _repository;
    private readonly ILessonResultRepository _lessonResults;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GrantExtraAttemptHandler(
        ISubmissionRepository repository,
        ILessonResultRepository lessonResults,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _lessonResults = lessonResults;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SubmissionResponseDto> Handle(
        GrantExtraAttemptCommand request,
        CancellationToken cancellationToken)
    {
        // כבר מסונן לפי בעלות המורה על השיעור. 404 ולא 403 — ר' LessonAccess.
        var submission = await _repository.GetByIdAsync(
            request.SubmissionId, request.TeacherId, cancellationToken);

        if (submission is null)
            throw new NotFoundException(nameof(Submission), request.SubmissionId);

        // ⚠️ האישור גובר על סף הציון אבל <b>לא</b> על נעילת שיעור: ציון סופי שכבר נמסר
        // היה משתנה מתחת לתלמידה. לשם כך יש פתיחה מחדש של הציון הסופי.
        if (await SubmissionLock.IsLockedAsync(_lessonResults, submission, cancellationToken))
            throw new BusinessRuleException(
                "לא ניתן לאשר הגשה נוספת — השיעור כבר סוכם לתלמידה או שהכיתה בארכיון. " +
                "כדי לאפשר זאת יש לפתוח מחדש את הציון הסופי של השיעור.");

        submission.GrantExtraAttempt(request.TeacherUserId, request.Reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SubmissionResponseDto>(submission);
    }
}

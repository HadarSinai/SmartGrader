using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Submissions.OverrideScore;

/// <summary>
/// דורס את ציון ההגשה ומתעד מי · מתי · למה.
/// <para>
/// עד כה ל-<c>Submission.Score</c> לא היה שום mutator ציבורי, וציון שגוי לא היה ניתן
/// לתיקון בשום דרך.
/// </para>
/// </summary>
public class OverrideScoreHandler
    : IRequestHandler<OverrideScoreCommand, SubmissionResponseDto>
{
    private readonly ISubmissionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public OverrideScoreHandler(
        ISubmissionRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SubmissionResponseDto> Handle(
        OverrideScoreCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await _repository.GetByIdAsync(
            request.SubmissionId, request.TeacherId, cancellationToken);

        if (submission is null)
            throw new NotFoundException(nameof(Submission), request.SubmissionId);

        // התקרה מגיעה מהתרגיל ולא מקבוע: בתרגיל בונוס היא מעל 100 — ר' Assignment.MaxScore.
        var maxScore = submission.Assignment?.MaxScore ?? Assignment.TotalPoints;

        if (request.Score < 0 || request.Score > maxScore)
            throw new BusinessRuleException($"הציון חייב להיות בין 0 ל-{maxScore}.");

        submission.OverrideScore(request.Score, maxScore, request.TeacherUserId, request.Reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SubmissionResponseDto>(submission);
    }
}

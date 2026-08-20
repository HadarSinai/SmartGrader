using MediatR;
using SmartGrader.Application.Dtos.LessonResults;

namespace SmartGrader.Application.UseCases.LessonResults.GetLessonScoreSuggestion;

/// <summary>
/// הקלט לדיאלוג "סיום שיעור" — ציון לכל תרגיל וממוצע כהצעה.
/// </summary>
public record GetLessonScoreSuggestionQuery(
    int StudentId,
    int LessonId,
    int? TeacherId
) : IRequest<LessonScoreSuggestionDto>;

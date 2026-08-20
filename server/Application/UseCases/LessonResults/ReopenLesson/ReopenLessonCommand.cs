using MediatR;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.LessonResults.ReopenLesson;

/// <summary>
/// פותחת מחדש ציון סופי של שיעור לתלמידה אחת.
/// <para>
/// ⚠️ עד כה <c>CompleteWith</c> זרק "Already completed" ו<b>ציון סופי שגוי לא היה ניתן
/// לתיקון בשום דרך</b>. הפתיחה גם משחררת את ההגשות של אותה תלמידה בשיעור מהנעילה.
/// </para>
/// </summary>
public record ReopenLessonCommand(
    int StudentId,
    int LessonId,
    int? TeacherId
) : IRequest<LessonResult>;

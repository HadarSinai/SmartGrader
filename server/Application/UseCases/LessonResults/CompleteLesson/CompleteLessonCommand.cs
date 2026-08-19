using MediatR;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.LessonResults.CompleteLesson;

// ⚠️ TeacherId לפני HasBonus בכוונה: פרמטר בלי ברירת מחדל חייב לבוא לפני פרמטר עם ברירת מחדל,
// וברירת מחדל כאן הייתה מחזירה בדיוק את הבאג — כל מורה מחוברת יכלה לקבוע ציון סופי לכל תלמידה
// בכל שיעור, כי לפקודה לא היה מזהה מורה בכלל.
public record CompleteLessonCommand(
    int StudentId,
    int LessonId,
    double FinalScore,
    int? TeacherId,
    bool HasBonus = false
) : IRequest<LessonResult>;

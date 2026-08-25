using MediatR;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.LessonResults.CompleteLesson;

/// <summary>
/// סיכום שיעור לתלמידה.
/// <para>
/// 🔴 <c>FinalScore</c> אינו הציון. הציון נגזר בשרת מההגשות
/// (<c>LessonScoreCalculator</c>); השדה כאן הוא <b>בקשת דריסה</b> בלבד, ומופעל רק כשהוא
/// שונה מהמחושב — ואז <c>OverrideReason</c> הוא חובה. עד לתיקון הזה הערך הזה נכתב כמו
/// שהוא, כלומר הציון הסופי היה מה שהדפדפן שלח.
/// </para>
/// <para>
/// ⚠️ <c>HasBonus</c> הוסר בכוונה: הוא הגיע מהלקוח וקבע את התקרה (150 במקום 100), כך
/// שסימון תיבה בדפדפן הרחיב את טווח הציון החוקי. עכשיו הוא נגזר מהתרגילים בפועל.
/// </para>
/// </summary>
/// <param name="TeacherId">
/// בעלות על השיעור — <c>OwnerScopeTeacherId</c>. <c>null</c> = מנהל/ת.
/// </param>
/// <param name="TeacherUserId">
/// מי מבצעת את הפעולה, מה-claims ולא מגוף הבקשה — אחרת יומן הביקורת הוא דיווח עצמי.
/// </param>
public record CompleteLessonCommand(
    int StudentId,
    int LessonId,
    int? TeacherId,
    int TeacherUserId,
    double? FinalScore,
    string? OverrideReason
) : IRequest<LessonResult>;

using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos.LessonResults;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Domain.Services;

namespace SmartGrader.Application.UseCases.LessonResults.GetLessonScoreSuggestion;

/// <summary>
/// אוסף את הציונים שהמערכת כבר חישבה בשיעור ומציע את הממוצע.
/// <para>
/// 🔴 עד כה כל ציון שהמנוע חישב נעצר בהגשה ולא הגיע לציון השיעור:
/// <c>CompleteLessonHandler</c> הזריק <c>ISubmissionRepository</c> אבל השתמש בו רק לבדיקת
/// סטטוסים חוסמים ומעולם לא קרא ציון, והמסך פתח את הדיאלוג עם <c>null</c>. התוצאה: המורה
/// חישבה ממוצע ביד לכל תלמידה בזמן שכל המספרים כבר היו במערכת.
/// </para>
/// </summary>
public class GetLessonScoreSuggestionHandler
    : IRequestHandler<GetLessonScoreSuggestionQuery, LessonScoreSuggestionDto>
{
    private readonly IAssignmentRepository _assignments;
    private readonly ISubmissionRepository _submissions;
    private readonly ILessonRepository _lessons;

    public GetLessonScoreSuggestionHandler(
        IAssignmentRepository assignments,
        ISubmissionRepository submissions,
        ILessonRepository lessons)
    {
        _assignments = assignments;
        _submissions = submissions;
        _lessons = lessons;
    }

    public async Task<LessonScoreSuggestionDto> Handle(
        GetLessonScoreSuggestionQuery request,
        CancellationToken ct)
    {
        // אותה בדיקת בעלות כמו בסיום שיעור עצמו. 404 ולא 403 — ר' LessonAccess.
        await LessonAccess.GetOwnedOrThrowAsync(_lessons, request.LessonId, request.TeacherId, ct);

        var assignments = await _assignments.GetByLessonIdAsync(request.LessonId, ct);
        var submissions = await _submissions.GetByStudentAndLessonAsync(
            request.StudentId, request.LessonId, ct);

        // הגשה אחת בדיוק לכל (תלמידה, תרגיל), אבל ההצמדה נעשית לפי המאוחרת ליתר ביטחון:
        // האינדקס הייחודי נוסף אחרי שכבר היו נתונים.
        var latestByAssignment = submissions
            .GroupBy(s => s.AssignmentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(s => s.SubmittedAt).First());

        var rows = assignments.Select(assignment =>
        {
            latestByAssignment.TryGetValue(assignment.Id, out var submission);

            return new AssignmentScoreDto
            {
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                Score = submission?.Score,
                Status = DescribeStatus(submission),
                IsBonus = assignment.IsBonus,
                BonusValue = assignment.IsBonus ? Math.Max(assignment.BonusValue, 0) : 0
            };
        }).ToList();

        // ⚠️ המספרים עצמם מגיעים מ-LessonScoreCalculator, אותה פונקציה שסיכום השיעור
        // משתמש בה. חישוב מקומי כאן — גם אם זהה היום — היה יוצר מצב שבו הדיאלוג מציג
        // מספר אחד והשרת דוחה אותו כ"חריגה" הדורשת סיבה.
        var summary = LessonScoreCalculator.Calculate(assignments, submissions);

        return new LessonScoreSuggestionDto
        {
            StudentId = request.StudentId,
            LessonId = request.LessonId,
            StudentName = submissions.FirstOrDefault()?.Student?.FullName,
            AssignmentScores = rows,
            SuggestedScore = summary.ComputedScore,
            GradedCount = summary.GradedCount,
            UngradedCount = summary.UngradedCount,
            BaseScore = summary.BaseScore,
            BonusPoints = summary.BonusPoints,
            MaxScore = summary.MaxScore
        };
    }

    /// <summary>למה אין ציון — הדיאלוג חייב להסביר, לא רק להשמיט שורה.</summary>
    private static string DescribeStatus(Submission? submission) => submission?.Status switch
    {
        null => "לא הוגש",
        SubmissionStatus.Done => "נבדק",
        SubmissionStatus.PendingAi or SubmissionStatus.ProcessingAi => "בבדיקה",
        SubmissionStatus.CompilationFailed => "שגיאת קומפילציה",
        SubmissionStatus.RequirementsNotMet => "לא עמדה בדרישות",
        SubmissionStatus.JudgeUnavailable => "תקלה במערכת הבדיקה",
        SubmissionStatus.AiFailed => "כשל בבדיקה — נדרש ציון ידני",
        _ => submission.Status.ToString()
    };
}

using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Domain.Services;

public class CompleteLessonHandler
    : IRequestHandler<CompleteLessonCommand, LessonResult>
{
    private readonly ILessonResultRepository _repository;
    private readonly ISubmissionRepository _submissions;
    private readonly IAssignmentRepository _assignments;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteLessonHandler(
        ILessonResultRepository repository,
        ISubmissionRepository submissions,
        IAssignmentRepository assignments,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _submissions = submissions;
        _assignments = assignments;
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

        // 🔴 השרת מחשב את הציון. עד לתיקון הזה הוא נלקח מגוף הבקשה כמו שהוא, והמקום היחיד
        // שבו הציון הסופי נגזר היה הדפדפן — בזמן שכל ציון להגשה בודדת נקבע בשרת בידי
        // ScoreCalculator, שהוא פונקציה טהורה בדיוק כדי שאף אחד לא יוכל להשפיע עליו.
        var assignments = await _assignments.GetByLessonIdAsync(command.LessonId, ct);
        var summary = LessonScoreCalculator.Calculate(assignments, submissions);

        // התקרה נגזרת מהתרגילים בפועל ולא ממה שהלקוח סימן: 100 ועוד סכום ה-BonusValue
        // של תרגילי הבונוס בשיעור. התקרה השטוחה 150 לא נגזרה משום דבר.
        var maxScore = summary.MaxScore;

        var result = await _repository.GetAsync(command.StudentId, command.LessonId, ct)
                     ?? LessonResult.Create(command.StudentId, command.LessonId);

        var isOverride = command.FinalScore.HasValue
                         && !LessonScoreCalculator.Matches(summary.ComputedScore, command.FinalScore.Value);

        if (!isOverride)
        {
            if (summary.ComputedScore is null)
                // ⚠️ שני מצבים שונים ומסר נפרד לכל אחד. שיעור שכולו בונוס לעולם לא ייסגר
                // לבד — אין בו תרגיל חובה שממנו נגזר בסיס — והודעה על "אף תרגיל לא נבדק"
                // הייתה שולחת את המורה לחפש הגשה תקועה שאינה קיימת.
                throw new BusinessRuleException(
                    summary.HasRequiredAssignment
                        ? "לא ניתן לסכם את השיעור — אף תרגיל חובה לא נבדק, ואין ציון מחושב. " +
                          "אפשר לקבוע ציון סופי ידנית, ואז יש לציין סיבה."
                        : "לא ניתן לסכם את השיעור — כל התרגילים בו הם בונוס, ואין ממה לחשב ציון בסיס. " +
                          "אפשר לקבוע ציון סופי ידנית, ואז יש לציין סיבה.");

            result.CompleteWith(summary.ComputedScore.Value, maxScore);
        }
        else
        {
            // הסיבה היא יומן הביקורת, בדיוק כמו בדריסת ציון של הגשה בודדת.
            if (string.IsNullOrWhiteSpace(command.OverrideReason))
                throw new BusinessRuleException(
                    $"הציון שהוזן ({command.FinalScore}) שונה מהציון שהמערכת חישבה " +
                    $"({summary.ComputedScore?.ToString() ?? "אין"}). יש לציין סיבה לשינוי.");

            // ArgumentOutOfRangeException מהישות היה חוזר כ-500. הבדיקה כאן מחזירה 400 עם
            // הסבר, והתקרה היא זו שנגזרה מהתרגילים.
            if (command.FinalScore!.Value < 0 || command.FinalScore.Value > maxScore)
                throw new BusinessRuleException(
                    $"הציון הסופי חייב להיות בין 0 ל-{maxScore}.");

            result.CompleteWithOverride(
                summary.ComputedScore,
                command.FinalScore.Value,
                command.TeacherUserId,
                command.OverrideReason,
                maxScore);
        }

        if (result.Id == 0)
            await _repository.AddAsync(result, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return result;
    }
}

using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Submissions.GrantExtraAttempt;

/// <summary>
/// אישור המורה לתלמידה להגיש שוב, מעל סף הציון.
/// </summary>
/// <param name="TeacherUserId">
/// מזהה המשתמש/ת המאשר/ת — נרשם בהגשה. ⚠️ בא מה-claims של הטוקן ולעולם לא מגוף הבקשה:
/// שדה בגוף הבקשה הופך את יומן הביקורת לדיווח עצמי.
/// </param>
public record GrantExtraAttemptCommand(
    int SubmissionId,
    int? TeacherId,
    int TeacherUserId,
    string Reason
) : IRequest<SubmissionResponseDto>;

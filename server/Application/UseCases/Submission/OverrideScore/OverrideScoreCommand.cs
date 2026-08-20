using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Submissions.OverrideScore;

/// <summary>
/// דריסת ציון ההגשה בידי המורה — רשת ביטחון, לא חלק מהמסלול הרגיל.
/// </summary>
/// <param name="TeacherUserId">מזהה המאשר/ת מה-claims של הטוקן, לא מגוף הבקשה.</param>
public record OverrideScoreCommand(
    int SubmissionId,
    int? TeacherId,
    int TeacherUserId,
    double Score,
    string Reason
) : IRequest<SubmissionResponseDto>;

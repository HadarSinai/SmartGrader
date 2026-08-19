using MediatR;
using SmartGrader.Application.Dtos.Submissions;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissions
{
    // TeacherId — סינון לפי בעלות המורה על השיעור שמתחת לתרגיל. null = מנהל/ת או תלמידה
    // שקוראת את ההגשות של עצמה. בלי הפרמטר הזה כל מורה קראה את ההגשות של תלמידות מורה אחרת.
    // IsStudentCaller — ר' GetSubmissionByIdQuery.
    public record GetSubmissionsQuery(int StudentId, int? TeacherId, bool IsStudentCaller)
        : IRequest<IReadOnlyList<SubmissionResponseDto>>;
}

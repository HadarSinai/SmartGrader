using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Lessons.DeleteLesson
{
    public class DeleteLessonHandler : IRequestHandler<DeleteLessonCommand, Unit>
    {
        private readonly ILessonRepository _repository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly ILessonResultRepository _lessonResultRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLessonHandler(
            ILessonRepository repository,
            IAssignmentRepository assignmentRepository,
            ISubmissionRepository submissionRepository,
            ILessonResultRepository lessonResultRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _assignmentRepository = assignmentRepository;
            _submissionRepository = submissionRepository;
            _lessonResultRepository = lessonResultRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
        {
            var lesson = await LessonAccess.GetOwnedOrThrowAsync(_repository, request.Id, request.TeacherId, cancellationToken);

            // ⚠️ עד לתיקון הזה זו הייתה מחיקה חשופה: EF מחק בשרשרת את התרגילים, כל ההגשות
            // (קוד, משוב AI וציונים) וכל הציונים הסופיים — בלי לשאול ובלי שהמחיקה תיחסם.
            // מרגע שיש עבודת תלמידות מתחת לשיעור, המחיקה נחסמת עם הסבר שאומר מה בדיוק חוסם.
            var submissionsCount = await _submissionRepository.CountByLessonIdAsync(request.Id, cancellationToken);
            var resultsCount = await _lessonResultRepository.CountByLessonIdAsync(request.Id, cancellationToken);

            if (submissionsCount > 0 || resultsCount > 0)
                throw new BusinessRuleException(
                    $"לא ניתן למחוק את השיעור — יש בו {DescribeWork(submissionsCount, resultsCount)}. " +
                    "מחיקה תמחק גם אותם לצמיתות, ולכן היא חסומה.");

            // אין עבודת תלמידות — מוחקים את התרגילים במפורש. הקשר עבר ל-Restrict, כך שמחיקה
            // מדורגת לא תקרה מעצמה, וזה בכוונה: כל מחיקה כאן היא החלטה מפורשת בקוד.
            // ⚠️ דרך lesson.Assignments ולא דרך GetByLessonIdAsync — האחרונה היא AsNoTracking,
            // ומחיקת עותק לא-מנוטר בזמן שה-Lesson המנוטר כבר טוען את אותם תרגילים מפילה את
            // ה-DbContext על התנגשות מפתח.
            foreach (var assignment in lesson.Assignments.ToList())
                await _assignmentRepository.DeleteAsync(assignment, cancellationToken);

            await _repository.DeleteAsync(lesson, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // IRequest בלי טיפוס מחזיר Unit
            return Unit.Value;
        }

        private static string DescribeWork(int submissions, int results)
        {
            var parts = new List<string>();
            if (submissions > 0) parts.Add($"{submissions} הגשות");
            if (results > 0) parts.Add($"{results} ציונים סופיים");
            return string.Join(" ו-", parts);
        }
    }
}

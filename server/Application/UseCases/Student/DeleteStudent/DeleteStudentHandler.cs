
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Students.DeleteStudent
{
    public class DeleteStudentHandler : IRequestHandler<DeleteStudentCommand>
    {
        private readonly IStudentRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly ILessonResultRepository _lessonResultRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteStudentHandler(
            IStudentRepository repository,
            IUserRepository userRepository,
            ISubmissionRepository submissionRepository,
            ILessonResultRepository lessonResultRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _userRepository = userRepository;
            _submissionRepository = submissionRepository;
            _lessonResultRepository = lessonResultRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            DeleteStudentCommand request,
            CancellationToken cancellationToken)
        {
            var student = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (student is null)
                throw new NotFoundException("Student", request.Id);

            // ⚠️ מחיקת תלמידה מוחקת בשרשרת גם את ההגשות והציונים הסופיים שלה — ולכן היא חסומה
            // כשיש כאלה. ארכוב הכיתה (SchoolClass.IsArchived) הוא הדרך להוציא תלמידה מהמערכת
            // בלי לאבד את מה שהיא עשתה.
            var submissionsCount = await _submissionRepository.CountByStudentIdAsync(request.Id, cancellationToken);
            var resultsCount = await _lessonResultRepository.CountByStudentIdAsync(request.Id, cancellationToken);

            if (submissionsCount > 0 || resultsCount > 0)
                throw new BusinessRuleException(
                    $"לא ניתן למחוק את {student.FullName} — יש לה {DescribeWork(submissionsCount, resultsCount)} " +
                    "שיימחקו לצמיתות יחד איתה. אפשר להעביר את הכיתה לארכיון במקום.");

            // Deleting a student also deletes her login account (User), if one exists
            if (student.UserId is int userId)
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user is not null)
                    await _userRepository.DeleteAsync(user, cancellationToken);
            }

            await _repository.DeleteAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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

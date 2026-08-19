using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.DeleteAssignment
{
    public class DeleteAssignmentHandler
        : IRequestHandler<DeleteAssignmentCommand, Unit>
    {
        private readonly IAssignmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILessonRepository _lessonRepository;
        private readonly ISubmissionRepository _submissionRepository;

        public DeleteAssignmentHandler(
            IAssignmentRepository repository,
            ILessonRepository lessonRepository,
            ISubmissionRepository submissionRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _lessonRepository = lessonRepository;
            _submissionRepository = submissionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            DeleteAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            // 1) לוודא שהשיעור קיים ובבעלות המורה
            await LessonAccess.GetOwnedOrThrowAsync(_lessonRepository, request.LessonId, request.TeacherId, cancellationToken);

            // 2) לוודא שהמשימה קיימת
            var assignment = await _repository.GetByIdAsync(request.AssignmentId, cancellationToken);
            if (assignment is null)
                throw new NotFoundException(nameof(Assignment), request.AssignmentId);

            // 3) לוודא שהמשימה הזו שייכת לשיעור נכון
            if (assignment.LessonId != request.LessonId)
                throw new NotFoundException(
                    "Assignment",
                    request.AssignmentId
                );

            // 4) לא מוחקים תרגיל שיש מתחתיו עבודת תלמידות — הקשר עבר ל-Restrict, וזו ההודעה
            //    שמסבירה מה חוסם במקום שגיאת FK גולמית מה-DB
            var submissionsCount = await _submissionRepository.CountByAssignmentIdAsync(
                request.AssignmentId, cancellationToken);

            if (submissionsCount > 0)
                throw new BusinessRuleException(
                    $"לא ניתן למחוק את התרגיל — יש בו {submissionsCount} הגשות. " +
                    "מחיקה תמחק גם את הקוד, המשוב והציונים שלהן לצמיתות, ולכן היא חסומה.");

            // 5) מחיקה
            await _repository.DeleteAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}

using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Teachers.DeleteTeacher
{
    public class DeleteTeacherHandler : IRequestHandler<DeleteTeacherCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteTeacherHandler(
            IUserRepository userRepository,
            ILessonRepository lessonRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _lessonRepository = lessonRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            DeleteTeacherCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

            if (user is null)
                throw new NotFoundException("Teacher", request.Id);

            // ⚠️ הבדיקה העצמית לפני בדיקת התפקיד ולא אחריה: הנקודה כולה היא
            // [Authorize(Roles = "Admin")], כך שהמוחקת היא תמיד מנהלת. אחרי בדיקת התפקיד
            // השורה הזו לא הייתה נגמרת אף פעם, והמנהלת שמוחקת את עצמה הייתה מקבלת
            // "ניתן למחוק מכאן חשבונות מורות בלבד" — נכון טכנית, ולא עונה על מה שקרה.
            if (user.Id == request.CurrentUserId)
                throw new BusinessRuleException(
                    "לא ניתן למחוק את החשבון שאיתו את מחוברת כרגע.");

            // המסך הזה מוחק מורות בלבד. תלמידה נמחקת דרך DeleteStudentHandler — שמוחק גם
            // את שורת ה-Student ואת העבודה שלה — ומנהלת אחרת אינה נמחקת מכאן בכלל.
            if (user.Role != UserRole.Teacher)
                throw new BusinessRuleException(
                    "ניתן למחוק מכאן חשבונות מורות בלבד.");

            // ⚠️ בלי השומר הזה המחיקה נופלת ברמת ה-DB (Restrict על Lesson.TeacherId ו-
            // Course.TeacherId) כשגיאה 500 סתומה במקום הודעה שמסבירה מה חוסם ובכמה.
            var lessons = await _lessonRepository.CountByTeacherIdAsync(user.Id, cancellationToken);
            var courses = await _courseRepository.CountByTeacherIdAsync(user.Id, cancellationToken);

            if (lessons > 0 || courses > 0)
                throw new BusinessRuleException(
                    $"לא ניתן למחוק את {user.FullName} — יש לה {DescribeWork(lessons, courses)} " +
                    "שיישארו בלי בעלים. יש להעביר או למחוק אותם קודם.");

            await _userRepository.DeleteAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static string DescribeWork(int lessons, int courses)
        {
            var parts = new List<string>();
            if (lessons > 0) parts.Add($"{lessons} שיעורים");
            if (courses > 0) parts.Add($"{courses} קורסים");
            return string.Join(" ו-", parts);
        }
    }
}

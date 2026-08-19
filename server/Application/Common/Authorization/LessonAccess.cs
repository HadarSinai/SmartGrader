using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Authorization
{
    /// <summary>
    /// Shared ownership check for Lesson and anything nested under it (Assignments, reports).
    /// Throws NotFoundException for both "missing" and "not yours" — indistinguishable on
    /// purpose, so lesson ids cannot be probed. 403 would leak existence; role mismatch is a
    /// separate concern handled by [Authorize(Roles=...)] on the controller.
    /// </summary>
    public static class LessonAccess
    {
        public static async Task<Lesson> GetOwnedOrThrowAsync(
            ILessonRepository repository,
            int lessonId,
            int? teacherId,
            CancellationToken ct)
        {
            var lesson = await repository.GetByIdAsync(lessonId, ct);

            if (lesson is null || (teacherId.HasValue && lesson.TeacherId != teacherId.Value))
                throw new NotFoundException(nameof(Lesson), lessonId);

            return lesson;
        }

        /// <summary>
        /// The scoping entry point for endpoints a student shares with a teacher
        /// (GetLessonById / GetAssignments / GetAssignmentById).
        /// <para>
        /// ⚠️ TeacherId is null for a student by design — which is why this overload exists:
        /// without a StudentId the check would simply not run, and every lesson in the school
        /// would be readable by anyone logged in. Exactly one of the two carries a value for a
        /// non-admin caller; Admin passes null for both and sees everything.
        /// </para>
        /// StudentId must come from the token claim, never from the request.
        /// </summary>
        public static async Task<Lesson> GetAccessibleOrThrowAsync(
            ILessonRepository lessonRepository,
            IStudentRepository studentRepository,
            int lessonId,
            int? teacherId,
            int? studentId,
            CancellationToken ct)
        {
            var lesson = await GetOwnedOrThrowAsync(lessonRepository, lessonId, teacherId, ct);

            if (studentId.HasValue &&
                !await IsAssignedToClassOfAsync(studentRepository, lesson, studentId.Value, ct))
                throw new NotFoundException(nameof(Lesson), lessonId);

            return lesson;
        }

        /// <summary>
        /// True when the lesson is assigned to the class the student belongs to. The class is
        /// read from the DB by student id — never taken from the request.
        /// </summary>
        public static async Task<bool> IsAssignedToClassOfAsync(
            IStudentRepository studentRepository,
            Lesson lesson,
            int studentId,
            CancellationToken ct)
        {
            var student = await studentRepository.GetByIdAsync(studentId, ct);
            return student is not null && IsAssignedToClass(lesson, student.ClassId);
        }

        /// <summary>
        /// Requires the lesson to have been loaded with its Classes collection
        /// (ILessonRepository.GetByIdAsync includes it).
        /// </summary>
        public static bool IsAssignedToClass(Lesson lesson, int classId) =>
            lesson.Classes.Any(c => c.Id == classId);
    }
}

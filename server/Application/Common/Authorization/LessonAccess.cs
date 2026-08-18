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
    }
}

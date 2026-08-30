using FluentAssertions;
using SmartGrader.Application.Common.BulkDelete;
using SmartGrader.Application.UseCases.Lessons.BulkDeleteLessons;
using Xunit;

namespace SmartGrader.UnitTests.Validation
{
    /// <summary>
    /// השערים על הבקשה עצמה. ⚠️ אחד לכל ארבעת המשאבים היה אותו טסט ארבע פעמים; הכלל
    /// זהה בכולם ונבדק כאן פעם אחת, על השיעורים.
    /// </summary>
    public class BulkDeleteValidatorTests
    {
        private static readonly BulkDeleteLessonsCommandValidator Validator = new();

        private static bool IsValid(int idCount) =>
            Validator.Validate(
                new BulkDeleteLessonsCommand(
                    Enumerable.Range(1, idCount).ToList(), TeacherId: 7))
                .IsValid;

        // ⚠️ בקשה ריקה אינה "מחיקה שהצליחה על אפס שורות" — היא כפתור שנלחץ בלי בחירה,
        // ותשובת הצלחה עליה נראית למורה כאילו משהו נמחק
        [Fact]
        public void Validator_RejectsAnEmptySelection()
        {
            IsValid(0).Should().BeFalse();
        }

        // בדיוק על התקרה — חוקי; מעליה — נדחה
        [Theory]
        [InlineData(1, true)]
        [InlineData(BulkDeleteRunner.MaxIdsPerRequest, true)]
        [InlineData(BulkDeleteRunner.MaxIdsPerRequest + 1, false)]
        public void Validator_CapsTheNumberOfIdsPerRequest(int idCount, bool expected)
        {
            IsValid(idCount).Should().Be(expected);
        }
    }
}

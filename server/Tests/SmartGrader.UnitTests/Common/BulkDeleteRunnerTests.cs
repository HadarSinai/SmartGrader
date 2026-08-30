using FluentAssertions;
using SmartGrader.Application.Common.BulkDelete;
using SmartGrader.Application.Common.Exceptions;
using Xunit;

namespace SmartGrader.UnitTests.Common
{
    /// <summary>
    /// מחיקה מרובה. ⚠️ הנבדק כאן אינו "כמה נמחקו" אלא <b>שסירוב אינו בולע את השאר</b>:
    /// בחירה של עשר שורות שארבע מהן מוגנות חייבת למחוק שש, לסרב לארבע, ולומר על כל אחת
    /// למה. שתי החלופות גרועות באותה מידה — לסרב לכל העשר, או למחוק שש ולדווח הצלחה.
    /// </summary>
    public class BulkDeleteRunnerTests
    {
        private static readonly CancellationToken None = CancellationToken.None;

        /// <summary>מחיקה מדומה שמסרבת למזהים שנקבעו מראש, ורושמת במה נגעה.</summary>
        private sealed class FakeDelete
        {
            private readonly HashSet<int> _refuse;
            private readonly HashSet<int> _missing;

            public FakeDelete(IEnumerable<int>? refuse = null, IEnumerable<int>? missing = null)
            {
                _refuse = new HashSet<int>(refuse ?? Array.Empty<int>());
                _missing = new HashSet<int>(missing ?? Array.Empty<int>());
            }

            public List<int> Attempted { get; } = new();

            public Task DeleteAsync(int id)
            {
                Attempted.Add(id);

                if (_refuse.Contains(id))
                    throw new BusinessRuleException($"לא ניתן למחוק את {id} — יש בה עבודה.");

                if (_missing.Contains(id))
                    throw new NotFoundException("Lesson", id);

                return Task.CompletedTask;
            }
        }

        // ── הכול נמחק ──

        // בלי שורה מוגנת: כל המזהים נמחקים ואין כשלים
        [Fact]
        public async Task Run_DeletesEveryId_WhenNothingIsRefused()
        {
            var delete = new FakeDelete();

            var result = await BulkDeleteRunner.RunAsync(new[] { 1, 2, 3 }, delete.DeleteAsync, None);

            result.DeletedIds.Should().Equal(1, 2, 3);
            result.Failures.Should().BeEmpty();
            result.DeletedCount.Should().Be(3);
        }

        // ── הצלחה חלקית ──

        // 🔴 שורה שסורבה אינה מבטלת את מה שנמחק ואינה עוצרת את מה שאחריה
        [Fact]
        public async Task Run_KeepsGoing_AfterARefusal()
        {
            var delete = new FakeDelete(refuse: new[] { 2 });

            var result = await BulkDeleteRunner.RunAsync(new[] { 1, 2, 3 }, delete.DeleteAsync, None);

            result.DeletedIds.Should().Equal(1, 3);
            result.FailedCount.Should().Be(1);
            delete.Attempted.Should().Equal(1, 2, 3);
        }

        // ⚠️ הסיבה עוברת כלשונה: היא מנוסחת במחיקה הבודדת ומונה מה בדיוק חוסם, וניסוח
        // מחדש כאן היה השני מבין שני מקורות אמת לאותה הודעה
        [Fact]
        public async Task Run_ReportsTheRefusalReasonVerbatim()
        {
            var delete = new FakeDelete(refuse: new[] { 2 });

            var result = await BulkDeleteRunner.RunAsync(new[] { 2 }, delete.DeleteAsync, None);

            result.Failures.Should().ContainSingle()
                .Which.Message.Should().Be("לא ניתן למחוק את 2 — יש בה עבודה.");
        }

        // שורה שנמחקה בינתיים במקום אחר אינה תקלה — היא שורה שאיננה
        [Fact]
        public async Task Run_ReportsAMissingRow_WithoutLeakingTheInternalMessage()
        {
            var delete = new FakeDelete(missing: new[] { 5 });

            var result = await BulkDeleteRunner.RunAsync(new[] { 5 }, delete.DeleteAsync, None);

            result.Failures.Should().ContainSingle()
                .Which.Message.Should().Be(BulkDeleteRunner.NotFoundMessage);
        }

        // ── מזהה כפול ──

        // ⚠️ אותו מזהה פעמיים היה נמחק פעם אחת ומדווח כלא-נמצא בפעם השנייה, כלומר
        // "1 נמחקה, 1 נכשלה" על שורה אחת שנמחקה בהצלחה
        [Fact]
        public async Task Run_CollapsesDuplicateIds()
        {
            var delete = new FakeDelete();

            var result = await BulkDeleteRunner.RunAsync(new[] { 4, 4, 4 }, delete.DeleteAsync, None);

            delete.Attempted.Should().Equal(4);
            result.DeletedIds.Should().Equal(4);
            result.Failures.Should().BeEmpty();
        }

        // ── מה שאינו סירוב ──

        // 🔴 תקלת מסד אינה "השורה הזו לא נמחקה" אלא מצב שאי אפשר לסמוך עליו, ולכן היא
        // מתפוצצת ואינה נאספת כשורה שנכשלה
        [Fact]
        public async Task Run_DoesNotSwallowAnUnexpectedFailure()
        {
            Task Explode(int id) => throw new InvalidOperationException("the connection died");

            var act = async () => await BulkDeleteRunner.RunAsync(new[] { 1 }, Explode, None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}

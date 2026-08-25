using SmartGrader.Domain.Entities;

namespace SmartGrader.UnitTests.Helpers
{
    /// <summary>
    /// בונה <see cref="Submission"/> במצב הרצוי דרך מכונת המצבים האמיתית בלבד:
    /// <c>Score</c> ניתן להשגה רק במסלול PendingAi → <see cref="Submission.MarkProcessingAi"/> →
    /// <see cref="Submission.MarkDone"/>. הרצף כתוב כאן פעם אחת ולא משוכפל בטסטים.
    /// </summary>
    public sealed class SubmissionBuilder
    {
        private readonly Submission _submission;

        public SubmissionBuilder(int studentId, int assignmentId)
        {
            _submission = new Submission(studentId, assignmentId, "class P { static void Main() { } }");
        }

        /// <summary>
        /// קובע את זמן ההגשה. ⚠️ רפלקציה נקודתית: <c>SubmittedAt</c> נכתב בבנאי מ-
        /// <c>DateTime.UtcNow</c> ואין שעון מוזרק, כך שבלי זה בדיקת "המאוחרת קובעת" הייתה
        /// תלויה ברזולוציית השעון — כלומר רועדת. חותמת זמן, לא מצב דומייני.
        /// </summary>
        public SubmissionBuilder SubmittedAt(DateTime utc)
        {
            typeof(Submission).GetProperty(nameof(Submission.SubmittedAt))!.SetValue(_submission, utc);
            return this;
        }

        /// <summary>
        /// קובע את מועד הניסיון האחרון — אותה קטגוריה בדיוק כמו <see cref="SubmittedAt"/>:
        /// חותמת שעון בלי שעון מוזרק. בלעדיה אי אפשר להגיע למסלול ההצלחה של
        /// <c>MarkPendingAi</c> (הגבלת הקצב תמיד תופסת הגשה שזה עתה נבנתה).
        /// </summary>
        public SubmissionBuilder LastSubmittedAt(DateTime utc)
        {
            typeof(Submission).GetProperty(nameof(Submission.LastSubmittedAt))!.SetValue(_submission, utc);
            return this;
        }

        /// <summary>מסיים בדיקה בהצלחה עם הציון הנתון — במסלול המצבים החוקי.</summary>
        public SubmissionBuilder Graded(double score)
        {
            _submission.MarkProcessingAi();
            _submission.MarkDone(
                new ScoreBreakdown(
                    TestPoints: score,
                    RulePoints: 0,
                    Total: score,
                    TestsAllocation: 100,
                    RulesAllocation: 0,
                    PassedTests: 1,
                    TotalTests: 1,
                    AllCorePassed: true),
                feedbackJson: null);
            return this;
        }

        /// <summary>בלי ציון — נשארת <see cref="SubmissionStatus.PendingAi"/> כפי שנוצרה.</summary>
        public Submission Build() => _submission;
    }
}

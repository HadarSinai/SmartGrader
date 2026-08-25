using FluentAssertions;
using SmartGrader.Domain.Entities;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Domain
{
    /// <summary>
    /// חוקי ההגשה: מכונת המצבים, הגשה חוזרת, אישור מורה ודריסת ציון.
    /// כל טסט משועתק מהערה קיימת ב-<c>Submission.cs</c>.
    /// </summary>
    public class SubmissionTests
    {
        private static readonly DateTime Past = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);

        // ── מכונת המצבים ──

        // הגשה חדשה: PendingAi, בלי ציון, ניסיון ראשון
        [Fact]
        public void NewSubmission_StartsPendingAi()
        {
            var submission = new SubmissionBuilder(7, 1).Build();

            submission.Status.Should().Be(SubmissionStatus.PendingAi);
            submission.Score.Should().BeNull();
            submission.AttemptNumber.Should().Be(1);
        }

        // אי אפשר לסיים בדיקה בלי לעבור דרך ProcessingAi
        [Fact]
        public void MarkDone_Throws_WhenNotProcessing()
        {
            var submission = new SubmissionBuilder(7, 1).Build();
            var breakdown = new ScoreBreakdown(80, 0, 80, 100, 0, 4, 5, true);

            var act = () => submission.MarkDone(breakdown, null);

            act.Should().Throw<InvalidOperationException>();
        }

        // סיום תקין: הציון מגיע מהפירוק, הסטטוס Done, ונרשם מועד בדיקה
        [Fact]
        public void MarkDone_SetsScoreFromBreakdown()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(88).Build();

            submission.Score.Should().Be(88);
            submission.Status.Should().Be(SubmissionStatus.Done);
            submission.ScoreBreakdown.Should().NotBeNull();
            submission.GradedAt.Should().NotBeNull();
        }

        // אי אפשר להתחיל עיבוד פעמיים
        [Fact]
        public void MarkProcessingAi_Throws_WhenAlreadyProcessing()
        {
            var submission = new SubmissionBuilder(7, 1).Build();
            submission.MarkProcessingAi();

            var act = () => submission.MarkProcessingAi();

            act.Should().Throw<InvalidOperationException>();
        }

        // ── הגשה חוזרת: כשל פתוח תמיד, הצלחה פתוחה רק מתחת לסף ──

        // כל מצבי הכשל פתוחים להגשה חוזרת
        [Fact]
        public void CanResubmit_IsTrue_ForCompilationFailure()
        {
            var submission = new SubmissionBuilder(7, 1).Build();
            submission.MarkCompilationFailed("CS1002");

            submission.CanResubmit(Assignment.DefaultRetryThreshold).Should().BeTrue();
        }

        [Fact]
        public void CanResubmit_IsTrue_ForAiFailure()
        {
            var submission = new SubmissionBuilder(7, 1).Build();
            submission.MarkProcessingAi();
            submission.MarkAiFailed("timeout");

            submission.CanResubmit(Assignment.DefaultRetryThreshold).Should().BeTrue();
        }

        [Fact]
        public void CanResubmit_IsTrue_ForUnmetRequirements()
        {
            var submission = new SubmissionBuilder(7, 1).Build();
            submission.MarkRequirementsNotMet();

            submission.CanResubmit(Assignment.DefaultRetryThreshold).Should().BeTrue();
        }

        // הגשה שעדיין בבדיקה אינה פתוחה להגשה חוזרת
        [Fact]
        public void CanResubmit_IsFalse_WhileStillProcessing()
        {
            var submission = new SubmissionBuilder(7, 1).Build();

            submission.CanResubmit(Assignment.DefaultRetryThreshold).Should().BeFalse();
        }

        // מתחת לסף פתוח; בדיוק על הסף — סגור (85 הוא "מספיק טוב")
        [Theory]
        [InlineData(84.9, true)]
        [InlineData(85, false)]
        [InlineData(100, false)]
        public void CanResubmit_ComparesScoreToRetryThreshold(double score, bool expected)
        {
            var submission = new SubmissionBuilder(7, 1).Graded(score).Build();

            submission.CanResubmit(85).Should().Be(expected);
        }

        // ── אישור המורה: מעל כל כלל, חד-פעמי, עם מעקב ──

        // אישור מורה פותח גם ציון מושלם
        [Fact]
        public void CanResubmit_IsTrue_WithUnusedExtraAttempt()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(100).Build();

            submission.GrantExtraAttempt(teacherUserId: 3, reason: "שיפור להצטיינות");

            submission.CanResubmit(85).Should().BeTrue();
            submission.ExtraAttemptGrantedByUserId.Should().Be(3);
        }

        // אישור בלי סיבה נדחה — הסיבה היא המעקב
        [Fact]
        public void GrantExtraAttempt_RequiresReason()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(100).Build();

            var act = () => submission.GrantExtraAttempt(3, "  ");

            act.Should().Throw<ArgumentException>();
        }

        // ── MarkPendingAi: מחזור חוזר ──

        // ניסיון חדש: המצב מתאפס, הניסיון הקודם מארכב, המונה עולה
        [Fact]
        public void MarkPendingAi_ResetsStateAndArchivesPreviousAttempt()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(50).LastSubmittedAt(Past).Build();

            submission.MarkPendingAi();

            submission.Status.Should().Be(SubmissionStatus.PendingAi);
            submission.Score.Should().BeNull();
            submission.ScoreBreakdown.Should().BeNull();
            submission.GradedAt.Should().BeNull();
            submission.AttemptNumber.Should().Be(2);
            submission.Attempts.Should().HaveCount(1);
        }

        // האישור החד-פעמי נצרך בהגשה הבאה — לא נשאר תלוי לניסיון שאחריה
        [Fact]
        public void MarkPendingAi_ConsumesExtraAttempt()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(100).LastSubmittedAt(Past).Build();
            submission.GrantExtraAttempt(3, "אישור חריג");

            submission.MarkPendingAi();

            submission.HasUnusedExtraAttempt.Should().BeFalse();
        }

        // שיעור שסוכם או כיתה בארכיון — נעול, גם כשהציון מתחת לסף
        [Fact]
        public void MarkPendingAi_Throws_WhenLocked()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(50).LastSubmittedAt(Past).Build();

            var act = () => submission.MarkPendingAi(isLocked: true);

            act.Should().Throw<InvalidOperationException>();
        }

        // ציון מעל הסף בלי אישור מורה — אין הגשה חוזרת
        [Fact]
        public void MarkPendingAi_Throws_WhenScoreAboveThreshold()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(90).LastSubmittedAt(Past).Build();

            var act = () => submission.MarkPendingAi(retryThreshold: 85);

            act.Should().Throw<InvalidOperationException>();
        }

        // ── הגבלת קצב: דקה בין ניסיונות ──

        // חצי דקה אחרי הניסיון האחרון — חסום; דקה שלמה — פתוח
        [Theory]
        [InlineData(30, true)]
        [InlineData(60, false)]
        [InlineData(90, false)]
        public void IsRateLimited_EnforcesMinuteBetweenAttempts(int secondsSinceLast, bool expected)
        {
            var submission = new SubmissionBuilder(7, 1).LastSubmittedAt(Past).Build();

            submission.IsRateLimited(Past.AddSeconds(secondsSinceLast)).Should().Be(expected);
        }

        // ── דריסת ציון בידי המורה ──

        // ציון מחוץ לטווח נדחה — התקרה היא של התרגיל, כולל בונוס
        [Theory]
        [InlineData(-0.5, 100)]
        [InlineData(100.5, 100)]
        [InlineData(121, 120)]
        public void OverrideScore_RejectsOutOfRange(double score, int maxScore)
        {
            var submission = new SubmissionBuilder(7, 1).Graded(64).Build();

            var act = () => submission.OverrideScore(score, maxScore, 3, "תיקון");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // תקרת בונוס מכובדת: 120 מתוך 120 חוקי
        [Fact]
        public void OverrideScore_AllowsBonusMaxScore()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(64).Build();

            submission.OverrideScore(120, maxScore: 120, teacherUserId: 3, reason: "בונוס מלא");

            submission.Score.Should().Be(120);
        }

        // ⚠️ הפירוק נמחק בדריסה: "בדיקות 64 · דרישות 0" ליד ציון ידני 90 משקר
        [Fact]
        public void OverrideScore_ClearsBreakdownAndRecordsAuditTrail()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(64).Build();

            submission.OverrideScore(90, 100, teacherUserId: 3, reason: "בדיקה ידנית");

            submission.Score.Should().Be(90);
            submission.ScoreBreakdown.Should().BeNull();
            submission.ScoreOverriddenByUserId.Should().Be(3);
            submission.ScoreOverrideReason.Should().Be("בדיקה ידנית");
            submission.Status.Should().Be(SubmissionStatus.Done);
        }

        // דריסה בלי סיבה נדחית — הסיבה היא המעקב
        [Fact]
        public void OverrideScore_RequiresReason()
        {
            var submission = new SubmissionBuilder(7, 1).Graded(64).Build();

            var act = () => submission.OverrideScore(90, 100, 3, "");

            act.Should().Throw<ArgumentException>();
        }
    }
}

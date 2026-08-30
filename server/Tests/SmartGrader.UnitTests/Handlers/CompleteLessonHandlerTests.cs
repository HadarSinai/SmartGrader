using FluentAssertions;
using NSubstitute;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Handlers
{
    /// <summary>
    /// סיכום שיעור לתלמידה. שני דברים נבדקים כאן, וכל אחד מהם היה באג אמיתי:
    /// <list type="number">
    /// <item>🔴 <b>השרת מחשב את הציון.</b> עד לתיקון, <c>FinalScore</c> נלקח מגוף הבקשה
    /// ונכתב כמו שהוא — כלומר הציון הסופי היה מה שהדפדפן שלח.</item>
    /// <item>⚠️ <b>אין לסכם שיעור שההגשה בו עדיין לא הגיעה למצב סופי</b> — למעט
    /// <c>AiFailed</c>, שמותר בכוונה כדי לאפשר ציון ידני.</item>
    /// </list>
    /// </summary>
    public class CompleteLessonHandlerTests
    {
        private const int StudentId = 5;
        private const int LessonId = 3;
        private const int OwnerTeacherId = 7;
        private const int OtherTeacherId = 8;
        private const int TeacherUserId = 71;

        private readonly ILessonResultRepository _results = Substitute.For<ILessonResultRepository>();
        private readonly ISubmissionRepository _submissions = Substitute.For<ISubmissionRepository>();
        private readonly IAssignmentRepository _assignments = Substitute.For<IAssignmentRepository>();
        private readonly ILessonRepository _lessons = Substitute.For<ILessonRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        public CompleteLessonHandlerTests()
        {
            _lessons.GetByIdAsync(LessonId, Arg.Any<CancellationToken>())
                .Returns(new TestEntities.TestLesson(LessonId, OwnerTeacherId));
        }

        private CompleteLessonHandler Handler() =>
            new(_results, _submissions, _assignments, _lessons, _unitOfWork);

        private static CompleteLessonCommand Command(
            double? finalScore = null,
            string? overrideReason = null,
            int? teacherId = OwnerTeacherId) =>
            new(StudentId, LessonId, teacherId, TeacherUserId, finalScore, overrideReason);

        private void GivenAssignments(params Assignment[] assignments) =>
            _assignments.GetByLessonIdAsync(LessonId, Arg.Any<CancellationToken>()).Returns(assignments);

        private void GivenSubmissions(params Submission[] submissions) =>
            _submissions.GetByStudentAndLessonAsync(StudentId, LessonId, Arg.Any<CancellationToken>())
                .Returns(submissions);

        // ── בעלות ──

        // מורה שהשיעור אינו שלה לא כותבת בו ציון סופי — ולא מגיעה בכלל לחישוב
        [Fact]
        public async Task Handle_Throws_ForNonOwningTeacher()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions(new SubmissionBuilder(StudentId, 1).Graded(80).Build());

            var act = async () => await Handler().Handle(
                Command(teacherId: OtherTeacherId), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // ── מה חוסם סיכום ──

        // הגשה שעדיין בבדיקה חוסמת — אחרת הציון הסופי נסגר על תרגיל שטרם נבדק.
        //
        // ⚠️ שימי לב לתרגיל הנבדק שלצידו, והוא אינו קישוט: בלעדיו יש בשיעור רק תרגיל אחד
        // בלי ציון, ואז ה-handler נכשל ממילא על "אף תרגיל לא נבדק" — כלומר הבדיקה הייתה
        // עוברת גם אילו בדיקת החסימה נמחקה כליל. כך זה התגלה בבדיקת השבירה המכוונת.
        [Fact]
        public async Task Handle_Throws_WhenASubmissionIsStillBeingGraded()
        {
            GivenAssignments(new TestAssignment(1), new TestAssignment(2));
            GivenSubmissions(
                new SubmissionBuilder(StudentId, 1).Graded(80).Build(),
                new SubmissionBuilder(StudentId, 2).Build());

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // כישלון קומפילציה חוסם — ההגשה ממתינה להגשה מחדש
        [Fact]
        public async Task Handle_Throws_WhenASubmissionFailedToCompile()
        {
            GivenAssignments(new TestAssignment(1), new TestAssignment(2));
            GivenSubmissions(
                new SubmissionBuilder(StudentId, 1).Graded(80).Build(),
                new SubmissionBuilder(StudentId, 2).CompilationFailed().Build());

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // ⚠️ AiFailed דווקא *אינו* חוסם — זה מה שמאפשר למורה לתת ציון ידני כשהמודל נפל
        [Fact]
        public async Task Handle_Completes_WhenTheOnlyUngradedSubmissionIsAiFailed()
        {
            GivenAssignments(new TestAssignment(1), new TestAssignment(2));
            GivenSubmissions(
                new SubmissionBuilder(StudentId, 1).Graded(80).Build(),
                new SubmissionBuilder(StudentId, 2).AiFailed().Build());

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.IsComplete.Should().BeTrue();
            result.FinalScore.Should().Be(80);
        }

        // ההגשה המאוחרת לתרגיל היא הקובעת — ניסיון ישן שנתקע בבדיקה אינו חוסם לנצח
        [Fact]
        [Trait("Rule", "G-25")]
        public async Task Handle_JudgesOnlyTheLatestSubmissionPerAssignment()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions(
                new SubmissionBuilder(StudentId, 1).SubmittedAt(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)).Build(),
                new SubmissionBuilder(StudentId, 1).SubmittedAt(new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc)).Graded(90).Build());

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.IsComplete.Should().BeTrue();
            result.FinalScore.Should().Be(90);
        }

        // ── 🔴 מי קובע את הציון ──

        // הציון נגזר מההגשות בשרת, גם כשהבקשה לא נשאה שום מספר
        [Fact]
        [Trait("Rule", "G-18")]
        public async Task Handle_ComputesTheScoreOnTheServer_WhenNoScoreWasEntered()
        {
            GivenAssignments(new TestAssignment(1), new TestAssignment(2));
            GivenSubmissions(
                new SubmissionBuilder(StudentId, 1).Graded(90).Build(),
                new SubmissionBuilder(StudentId, 2).Graded(70).Build());

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.FinalScore.Should().Be(80);
            result.IsFinalScoreOverridden.Should().BeFalse();
        }

        // מספר שזהה למחושב אינו דריסה — ואינו דורש סיבה
        [Fact]
        [Trait("Rule", "G-22")]
        public async Task Handle_TreatsAMatchingScoreAsNoOverride()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions(new SubmissionBuilder(StudentId, 1).Graded(80).Build());

            var result = await Handler().Handle(Command(finalScore: 80), CancellationToken.None);

            result.IsFinalScoreOverridden.Should().BeFalse();
            result.FinalScore.Should().Be(80);
        }

        // 🔴 מספר ששונה מהמחושב ובלי סיבה נדחה. זו השורה שמונעת מהלקוח לקבוע ציון בשקט.
        [Fact]
        public async Task Handle_Throws_WhenTheEnteredScoreDiffersAndNoReasonWasGiven()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions(new SubmissionBuilder(StudentId, 1).Graded(80).Build());

            var act = async () => await Handler().Handle(Command(finalScore: 100), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // דריסה מנומקת נרשמת במלואה — הציון, המחושב שלצידו, המנמקת והסיבה
        [Fact]
        [Trait("Rule", "G-24")]
        public async Task Handle_RecordsTheOverride_WithBothScoresAndItsAuthor()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions(new SubmissionBuilder(StudentId, 1).Graded(80).Build());

            var result = await Handler().Handle(
                Command(finalScore: 95, overrideReason: "השלימה בעל פה"), CancellationToken.None);

            result.FinalScore.Should().Be(95);
            result.ComputedScore.Should().Be(80);
            result.FinalScoreOverriddenByUserId.Should().Be(TeacherUserId);
            result.FinalScoreOverrideReason.Should().Be("השלימה בעל פה");
        }

        // ── התקרה נגזרת מהתרגילים, לא מהלקוח ──

        // בלי תרגיל בונוס התקרה היא 100, וציון מעליה נדחה בהסבר במקום ב-500
        [Fact]
        [Trait("Rule", "G-21")]
        public async Task Handle_Throws_WhenTheOverrideExceedsTheCeiling()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions(new SubmissionBuilder(StudentId, 1).Graded(80).Build());

            var act = async () => await Handler().Handle(
                Command(finalScore: 120, overrideReason: "סיבה"), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // ⚠️ עם תרגיל בונוס התקרה עולה ל-100 ועוד ה-BonusValue שלו — מפני שכך התרגילים
        // בנויים, ולא מפני שתיבה סומנה בדפדפן ולא לתקרה שטוחה של 150
        [Fact]
        [Trait("Rule", "G-21")]
        public async Task Handle_RaisesTheCeiling_ByTheBonusValue()
        {
            GivenAssignments(
                new TestAssignment(1),
                new TestAssignment(2, isBonus: true, bonusValue: 20));
            GivenSubmissions(new SubmissionBuilder(StudentId, 1).Graded(80).Build());

            var result = await Handler().Handle(
                Command(finalScore: 120, overrideReason: "כולל בונוס"), CancellationToken.None);

            result.FinalScore.Should().Be(120);
        }

        // ⚠️ ואותו שיעור עצמו דוחה 121: התקרה היא הבונוס שהמורה הזינה, לא מספר עגול
        // שנבחר פעם אחת. במודל הקודם 150 היה עובר כאן.
        [Fact]
        [Trait("Rule", "G-21")]
        public async Task Handle_Throws_WhenTheOverrideExceedsTheBonusCeiling()
        {
            GivenAssignments(
                new TestAssignment(1),
                new TestAssignment(2, isBonus: true, bonusValue: 20));
            GivenSubmissions(new SubmissionBuilder(StudentId, 1).Graded(80).Build());

            var act = async () => await Handler().Handle(
                Command(finalScore: 150, overrideReason: "סיבה"), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // ── אין ממה לחשב ──

        // אף תרגיל לא נבדק ולא הוזן ציון — נדחה, ולא נסגר על אפס
        [Fact]
        [Trait("Rule", "G-20")]
        public async Task Handle_Throws_WhenNothingIsGradedAndNoScoreWasEntered()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions();

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // אף תרגיל לא נבדק, אבל המורה קבעה ציון ונימקה — מותר
        [Fact]
        [Trait("Rule", "G-20")]
        public async Task Handle_AllowsAReasonedScore_WhenNothingIsGraded()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions();

            var result = await Handler().Handle(
                Command(finalScore: 70, overrideReason: "נבחנה בעל פה"), CancellationToken.None);

            result.FinalScore.Should().Be(70);
            result.ComputedScore.Should().BeNull();
        }

        // ── שמירה ──

        // סיכום ראשון יוצר שורה חדשה ושומר אותה
        [Fact]
        public async Task Handle_AddsAndSavesTheResult_WhenNoneExists()
        {
            GivenAssignments(new TestAssignment(1));
            GivenSubmissions(new SubmissionBuilder(StudentId, 1).Graded(80).Build());

            await Handler().Handle(Command(), CancellationToken.None);

            await _results.Received().AddAsync(Arg.Any<LessonResult>(), Arg.Any<CancellationToken>());
            await _unitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}

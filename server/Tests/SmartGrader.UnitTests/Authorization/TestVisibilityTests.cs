using FluentAssertions;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Submissions;
using Xunit;

namespace SmartGrader.UnitTests.Authorization
{
    /// <summary>
    /// הבדיקה החשובה ביותר בכל החבילה. כאן באג אינו ציון שגוי אלא <b>דליפה של התשובה
    /// לתרגיל</b> — ותלמידה שראתה את הפלט הצפוי לא צריכה לפתור כלום.
    /// <para>
    /// ⚠️ הסתרה בתבנית Angular אינה בקרה: ה-payload כבר בדפדפן ונקרא ב-DevTools.
    /// לכן כל טסט כאן בודק את מה שבאמת <b>עוזב את השרת</b>.
    /// </para>
    /// </summary>
    public class TestVisibilityTests
    {
        private const string SecretInput = "SECRET-INPUT-42";
        private const string SecretExpected = "SECRET-EXPECTED-99";

        private static AssignmentResponseDto Assignment() => new()
        {
            Id = 1,
            Tests = new List<TestCaseDto>
            {
                new() { Input = "1", Expected = "2", IsSample = true },
                new() { Input = SecretInput, Expected = SecretExpected, IsSample = false }
            },
            ReferenceSolution = new List<ReferenceSolutionFileDto>
            {
                new() { FileName = "Solution.cs", Content = "// the full answer" }
            }
        };

        private static SubmissionResponseDto Submission() => new()
        {
            Id = 1,
            TestResults = new List<TestCaseResultDto>
            {
                new() { Input = "1", Expected = "2", Actual = "2", Passed = true, IsSample = true },
                new()
                {
                    Input = SecretInput,
                    Expected = SecretExpected,
                    Actual = SecretInput,      // התלמידה הדפיסה את הקלט — כך הוא חוזר דרך stdout
                    Error = SecretExpected,    // ...ודרך stderr
                    Passed = false,
                    IsSample = false
                }
            }
        };

        // ── תרגיל: מה שהתלמידה מקבלת ──

        // מקרה בדיקה מוסתר לא נשלח כלל — לא מרוקן, פשוט לא שם
        [Fact]
        public void Redact_RemovesHiddenTestsEntirely_ForStudent()
        {
            var dto = TestVisibility.Redact(Assignment(), isStudentCaller: true);

            dto.Tests.Should().HaveCount(1);
            dto.Tests.Should().OnlyContain(t => t.IsSample);
        }

        // ⚠️ הפתרון לדוגמה הוא התשובה המלאה — מרוקן, לא מסונן
        [Fact]
        public void Redact_ClearsReferenceSolution_ForStudent()
        {
            var dto = TestVisibility.Redact(Assignment(), isStudentCaller: true);

            dto.ReferenceSolution.Should().BeEmpty();
        }

        // מורה ומנהלת מקבלות את התרגיל המלא — הסתרה מהן הייתה שוברת את המסך שלהן
        [Fact]
        public void Redact_KeepsEverything_ForTeacher()
        {
            var dto = TestVisibility.Redact(Assignment(), isStudentCaller: false);

            dto.Tests.Should().HaveCount(2);
            dto.ReferenceSolution.Should().HaveCount(1);
        }

        // ── ⚠️ נתיב הדליפה שהכי קל לשכוח: endpoint של רשימה ──

        // גם ברשימת תרגילים, ולא רק בתרגיל בודד
        [Fact]
        public void Redact_AppliesToEveryItemInAList()
        {
            IReadOnlyList<AssignmentResponseDto> list = new[] { Assignment(), Assignment() };

            var result = TestVisibility.Redact(list, isStudentCaller: true);

            result.Should().OnlyContain(d => d.Tests.Count == 1 && d.ReferenceSolution.Count == 0);
        }

        [Fact]
        public void Redact_LeavesListIntact_ForTeacher()
        {
            IReadOnlyList<AssignmentResponseDto> list = new[] { Assignment(), Assignment() };

            var result = TestVisibility.Redact(list, isStudentCaller: false);

            result.Should().OnlyContain(d => d.Tests.Count == 2 && d.ReferenceSolution.Count == 1);
        }

        // ── תוצאות הגשה: הסיכום נשמר, הפרטים לא ──

        // "עברו 3 מתוך 5" חייב להישאר כן — ולכן Passed שורד את ההסתרה
        [Fact]
        public void RedactTestResults_KeepsPassedFlag_SoTheSummaryStaysHonest()
        {
            var dto = TestVisibility.RedactTestResults(Submission(), isStudentCaller: true);

            dto.TestResults.Should().HaveCount(2);
            dto.TestResults[1].Passed.Should().BeFalse();
            dto.TestResults[1].IsHidden.Should().BeTrue();
        }

        // ⚠️ כל ארבעת השדות מרוקנים — כולל Actual ו-Error, שדרכם הקלט חוזר אם התלמידה מדפיסה אותו
        [Fact]
        public void RedactTestResults_BlanksEveryFieldTheSecretCanTravelThrough()
        {
            var dto = TestVisibility.RedactTestResults(Submission(), isStudentCaller: true);
            var hidden = dto.TestResults[1];

            hidden.Input.Should().BeEmpty();
            hidden.Expected.Should().BeEmpty();
            hidden.Actual.Should().BeEmpty();
            hidden.Error.Should().BeNull();
        }

        // 🔴 הבדיקה הכוללת: הסוד לא מופיע בשום שדה של ה-DTO כולו
        [Fact]
        public void RedactTestResults_LeavesNoTraceOfTheSecretAnywhere()
        {
            var dto = TestVisibility.RedactTestResults(Submission(), isStudentCaller: true);

            var everything = string.Join(
                "|",
                dto.TestResults.SelectMany(r => new[] { r.Input, r.Expected, r.Actual, r.Error ?? "" }));

            everything.Should().NotContain(SecretInput);
            everything.Should().NotContain(SecretExpected);
        }

        // מקרה דוגמה נשאר שלם — התלמידה כבר רואה אותו בניסוח המטלה
        [Fact]
        public void RedactTestResults_LeavesSampleRowsUntouched()
        {
            var dto = TestVisibility.RedactTestResults(Submission(), isStudentCaller: true);
            var sample = dto.TestResults[0];

            sample.Input.Should().Be("1");
            sample.Expected.Should().Be("2");
            sample.IsHidden.Should().BeFalse();
        }

        // מורה רואה גם את המוסתרים — היא כתבה אותם
        [Fact]
        public void RedactTestResults_KeepsEverything_ForTeacher()
        {
            var dto = TestVisibility.RedactTestResults(Submission(), isStudentCaller: false);

            dto.TestResults[1].Input.Should().Be(SecretInput);
            dto.TestResults[1].IsHidden.Should().BeFalse();
        }

        // ⚠️ אותו נתיב דליפה גם ברשימת הגשות ובפיד ההתראות, שמשתמשים באותו DTO
        [Fact]
        public void RedactTestResults_AppliesToEveryItemInAList()
        {
            IReadOnlyList<SubmissionResponseDto> list = new[] { Submission(), Submission() };

            var result = TestVisibility.RedactTestResults(list, isStudentCaller: true);

            result.Should().OnlyContain(d => d.TestResults[1].Input == "" && d.TestResults[1].IsHidden);
        }

        [Fact]
        public void RedactTestResults_LeavesListIntact_ForTeacher()
        {
            IReadOnlyList<SubmissionResponseDto> list = new[] { Submission(), Submission() };

            var result = TestVisibility.RedactTestResults(list, isStudentCaller: false);

            result.Should().OnlyContain(d => d.TestResults[1].Input == SecretInput);
        }

        // ── fail closed ──

        // 🔴 ברירת המחדל של IsSample היא false, ולכן שורה ישנה שנשמרה לפני שהשדה היה קיים
        // מתפרשת כמוסתרת. דגל חדש חייב תמיד להיפתח למצב הבטוח.
        [Fact]
        public void RedactTestResults_TreatsRowsWithDefaultFlagAsHidden()
        {
            var dto = new SubmissionResponseDto
            {
                TestResults = new List<TestCaseResultDto>
                {
                    new() { Input = SecretInput, Expected = SecretExpected, Passed = true }
                }
            };

            var result = TestVisibility.RedactTestResults(dto, isStudentCaller: true);

            result.TestResults[0].IsHidden.Should().BeTrue();
            result.TestResults[0].Input.Should().BeEmpty();
        }

        [Fact]
        public void Redact_TreatsTestsWithDefaultFlagAsHidden()
        {
            var dto = new AssignmentResponseDto
            {
                Tests = new List<TestCaseDto> { new() { Input = SecretInput, Expected = SecretExpected } }
            };

            TestVisibility.Redact(dto, isStudentCaller: true).Tests.Should().BeEmpty();
        }

        // ── הדרישות המבניות אינן סוד ──

        // ⚠️ בניגוד למקרי בדיקה — הדרישה נכתבה בניסוח המטלה ("חובה רקורסיה"), ובלעדיה
        // התלמידה לא יודעת מה נדרש ממנה. אסור שהסתרה תגלוש לכאן.
        [Fact]
        public void Redact_KeepsStructuralRules_ForStudent()
        {
            var dto = Assignment();
            dto.StructuralRules = new List<StructuralRuleDto> { new() };

            TestVisibility.Redact(dto, isStudentCaller: true).StructuralRules.Should().HaveCount(1);
        }
    }
}

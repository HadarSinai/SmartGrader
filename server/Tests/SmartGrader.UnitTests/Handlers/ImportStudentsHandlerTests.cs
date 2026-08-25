using AutoMapper;
using ClosedXML.Excel;
using FluentAssertions;
using NSubstitute;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Common.Mapping;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Application.UseCases.Students.ImportStudents;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Handlers
{
    /// <summary>
    /// ייבוא תלמידות מקובץ Excel. 🔴 הכלל שמנהל את ה-handler: <b>הצלחה חלקית</b> — שורה
    /// אחת פסולה אינה מבטלת את כל היתר. מורה שמעלה 30 שורות ומקבלת "נכשל" בגלל שורה 17
    /// תתקן ותעלה שוב, וכל 29 האחרות ייווצרו פעמיים.
    /// <para>
    /// ⚠️ הבדיקות כאן בונות קובץ xlsx אמיתי בזיכרון. ClosedXML אינו מדומה — הוא הפורמט
    /// עצמו, ו-mock שלו היה בודק את ה-mock.
    /// </para>
    /// </summary>
    public class ImportStudentsHandlerTests
    {
        private const string ClassName = "י\"א 3";
        private const string ValidPassword = "Sod1234Abc";

        private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly ISchoolClassRepository _classes = Substitute.For<ISchoolClassRepository>();
        private readonly IPasswordHasherService _hasher = Substitute.For<IPasswordHasherService>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private readonly IMapper _mapper =
            new MapperConfiguration(cfg => cfg.AddProfile<StudentProfile>()).CreateMapper();

        private ImportStudentsHandler Handler() =>
            new(_students, _users, _classes, _hasher, _unitOfWork, _mapper);

        /// <summary>
        /// בונה קובץ xlsx בזיכרון: שורת כותרת ואחריה השורות שנמסרו, בסדר
        /// שם מלא · כיתה · שם משתמש · סיסמה. תשתית בנייה, לא לוגיקת בדיקה.
        /// </summary>
        private static Stream Workbook(params string[][] rows)
        {
            var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet("תלמידות");

            sheet.Cell(1, 1).Value = "שם מלא";
            sheet.Cell(1, 2).Value = "כיתה";
            sheet.Cell(1, 3).Value = "שם משתמש";
            sheet.Cell(1, 4).Value = "סיסמה";

            for (var r = 0; r < rows.Length; r++)
                for (var c = 0; c < rows[r].Length; c++)
                    sheet.Cell(r + 2, c + 1).Value = rows[r][c];

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        private static string[] Row(string fullName, string className = ClassName,
            string username = "", string password = "") =>
            new[] { fullName, className, username, password };

        private void GivenTheClassExists(int id = 20, bool isArchived = false) =>
            _classes.GetByNameAndYearAsync(ClassName, Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(TestEntities.Class(id, isArchived));

        private Task<ImportStudentsResultDto> ImportAsync(Stream file) =>
            Handler().Handle(new ImportStudentsCommand(file), CancellationToken.None);

        // ── קובץ פסול ──

        // מה שאינו חוברת עבודה נדחה בהודעה ולא בקריסה
        [Fact]
        public async Task Handle_Throws_ForAFileThatIsNotAWorkbook()
        {
            var notAWorkbook = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            var act = async () => await ImportAsync(notAWorkbook);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // ── 🔴 הצלחה חלקית ──

        // שורה פסולה אחת אינה מבטלת את השורות התקינות
        [Fact]
        public async Task Handle_CreatesTheValidRows_AndReportsOnlyTheBadOne()
        {
            GivenTheClassExists();
            var file = Workbook(Row("רותי לוי"), Row(""), Row("שרה כהן"));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(2);
            result.Errors.Should().HaveCount(1);
            await _unitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // ⚠️ מספר השורה הוא של הקובץ, לא של הרשומה — כדי שהמורה תמצא אותה בגיליון.
        // שורת הנתונים השנייה היא שורה 3 בקובץ, כי שורה 1 היא הכותרת.
        [Fact]
        public async Task Handle_ReportsTheRowNumberFromTheFile()
        {
            GivenTheClassExists();
            var file = Workbook(Row("רותי לוי"), Row(""), Row("שרה כהן"));

            var result = await ImportAsync(file);

            result.Errors[0].RowNumber.Should().Be(3);
        }

        // שורה ריקה לגמרי מדולגת בשקט ואינה נספרת כשגיאה
        [Fact]
        public async Task Handle_SkipsFullyEmptyRowsSilently()
        {
            GivenTheClassExists();
            var file = Workbook(Row("רותי לוי"), new[] { "", "", "", "" }, Row("שרה כהן"));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(2);
            result.Errors.Should().BeEmpty();
        }

        // כשכל השורות נפסלו אין מה לשמור
        [Fact]
        public async Task Handle_SavesNothing_WhenEveryRowFailed()
        {
            GivenTheClassExists();
            var file = Workbook(Row(""), Row(""));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(0);
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // ── 🔴 מדיניות הסיסמה משותפת, לא משוכפלת ──

        // סיסמה עם אותיות עבריות נדחית גם בייבוא. הגרסה הקודמת מימשה את הכללים מחדש
        // והשמיטה בדיוק את הכלל הזה — תלמידה נכנסה עם סיסמה שהטופס מצהיר שאינה חוקית.
        [Fact]
        public async Task Handle_AppliesTheSharedPasswordPolicy_IncludingHebrewLetters()
        {
            GivenTheClassExists();
            var file = Workbook(Row("רותי לוי", username: "ruti", password: "Sod1234אבג"));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(0);
            result.Errors.Should().HaveCount(1);
        }

        // ── שמות משתמש ──

        // אותו שם משתמש פעמיים באותו קובץ — השנייה נפסלת, הראשונה נוצרת
        [Fact]
        public async Task Handle_RejectsAUsernameThatRepeatsInsideTheFile()
        {
            GivenTheClassExists();
            var file = Workbook(
                Row("רותי לוי", username: "ruti", password: ValidPassword),
                Row("רות כהן", username: "ruti", password: ValidPassword));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(1);
            result.Errors.Should().HaveCount(1);
        }

        // שם משתמש שכבר קיים במערכת נפסל
        [Fact]
        public async Task Handle_RejectsAUsernameThatAlreadyExists()
        {
            GivenTheClassExists();
            _users.ExistsByUsernameAsync("ruti", Arg.Any<CancellationToken>()).Returns(true);
            var file = Workbook(Row("רותי לוי", username: "ruti", password: ValidPassword));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(0);
            result.Errors.Should().HaveCount(1);
        }

        // חשבון הוא רשות: שם וכיתה בלבד יוצרים תלמידה בלי משתמש
        [Fact]
        public async Task Handle_CreatesAStudentWithoutAnAccount()
        {
            GivenTheClassExists();
            var file = Workbook(Row("רותי לוי"));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(1);
            await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        // ── כיתות ──

        // כיתה שאינה קיימת בשנה הנוכחית נוצרת אוטומטית
        [Fact]
        public async Task Handle_CreatesAClassThatDoesNotExistYet()
        {
            _classes.GetByNameAndYearAsync(ClassName, Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((SchoolClass?)null);
            var file = Workbook(Row("רותי לוי"));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(1);
            await _classes.Received().AddAsync(Arg.Any<SchoolClass>(), Arg.Any<CancellationToken>());
        }

        // ⚠️ כיתה בארכיון היא שנת לימודים שהתגלגלה — אין לשייך אליה תלמידות חדשות
        [Fact]
        public async Task Handle_RejectsAnArchivedClass()
        {
            GivenTheClassExists(isArchived: true);
            var file = Workbook(Row("רותי לוי"));

            var result = await ImportAsync(file);

            result.CreatedCount.Should().Be(0);
            result.Errors.Should().HaveCount(1);
        }
    }
}

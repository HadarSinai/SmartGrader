using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmartGrader.Domain.Entities
{
    public class Assignment
    {
        public int Id { get; private set; }
        public int LessonId { get;  set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsBonus { get; set; }
        public double BonusValue { get; set; }
        public string MethodName { get; set; } = "";
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        //הכנסת השאלה
        public string TestsJson { get; private set; } = "[]";
        public string ExpectedFilesJson { get; private set; } = "[]";
        public Lesson Lesson { get; set; }
        public ICollection<Submission> Submissions { get; set; }
        protected Assignment() { }
        [NotMapped]
        public List<TestCase> Tests
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TestsJson))
                    return new List<TestCase>();

                try
                {
                    return JsonSerializer.Deserialize<List<TestCase>>(TestsJson)
                           ?? new List<TestCase>();
                }
                catch
                {
                    // אם יש דאטה מקולקל ב־DB – שלא יפיל את השרת
                    return new List<TestCase>();
                }
            }
            private set
            {
                TestsJson = JsonSerializer.Serialize(value ?? new List<TestCase>());
            }
        }
        public void AddTest(string input, string expected)
        {
            var list = Tests; // קורא מה-JSON
            list.Add(new TestCase { Input = input, Expected = expected });
            Tests = list;     // כותב חזרה ל-JSON (ישמר ב-DB)
        }

        public void SetTests(List<TestCase>? tests)
        {
            Tests = tests ?? new List<TestCase>();
        }

        [NotMapped]
        public List<ExpectedFile> ExpectedFiles
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExpectedFilesJson))
                    return new List<ExpectedFile>();

                try
                {
                    return JsonSerializer.Deserialize<List<ExpectedFile>>(ExpectedFilesJson)
                           ?? new List<ExpectedFile>();
                }
                catch
                {
                    // אם יש דאטה מקולקל ב־DB – שלא יפיל את השרת
                    return new List<ExpectedFile>();
                }
            }
            private set
            {
                ExpectedFilesJson = JsonSerializer.Serialize(value ?? new List<ExpectedFile>());
            }
        }

        public void SetExpectedFiles(List<ExpectedFile>? expectedFiles)
        {
            ExpectedFiles = expectedFiles ?? new List<ExpectedFile>();
        }
    }
}

       

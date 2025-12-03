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
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public string TestsJson { get; private set; } = "[]";
        public Lesson Lesson { get; set; }
        public ICollection<Submission> Submissions { get; set; }

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
    }
}

       

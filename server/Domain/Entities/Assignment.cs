using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmartGrader.Domain.Entities
{
    public class Assignment
    {
        /// <summary>סף הציון שמתחתיו התלמידה רשאית להגיש שוב, כברירת מחדל.</summary>
        public const int DefaultRetryThreshold = 85;

        /// <summary>סך הנקודות שרובריקת תרגיל <b>רגיל</b> חייבת להסתכם בהן.</summary>
        public const int TotalPoints = 100;

        public int Id { get; private set; }
        public int LessonId { get;  set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsBonus { get; set; }
        public double BonusValue { get; set; }
        public string MethodName { get; set; } = "";
        public GradingMode GradingMode { get; set; } = GradingMode.FullProgram;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        //הכנסת השאלה
        public string TestsJson { get; private set; } = "[]";
        public string ExpectedFilesJson { get; private set; } = "[]";

        /// <summary>
        /// הפתרון לדוגמה של המורה, כ-JSON של <see cref="ReferenceSolutionFile"/>.
        /// ⚠️ התשובה המלאה לתרגיל — לא נשלח לתלמידה בשום מסלול. ר' TestVisibility.
        /// </summary>
        public string ReferenceSolutionJson { get; private set; } = "[]";

        /// <summary>
        /// הדרישות המבניות של התרגיל, כ-JSON של <see cref="StructuralRule"/> — אותה תבנית
        /// בדיוק כמו <see cref="TestsJson"/>.
        /// </summary>
        public string StructuralRulesJson { get; private set; } = "[]";

        /// <summary>
        /// כמה מתוך 100 הנקודות מוקצות למקרי הבדיקה. היתר מתחלק בין הדרישות המנוקדות.
        /// <para>
        /// ברירת המחדל 100 היא ההתנהגות הקיימת: תרגיל בלי דרישות מנוקדות מנוקד על הטסטים
        /// בלבד, בדיוק כמו לפני המנוע הזה.
        /// </para>
        /// <para>
        /// <b>0 הוא ערך חוקי</b> — תרגיל מחלקות אין לו מה להריץ והוא מנוקד על המבנה בלבד.
        /// </para>
        /// </summary>
        public int TestsAllocation { get; set; } = TotalPoints;

        /// <summary>
        /// תקרת הציון של התרגיל — וגם הסכום שהרובריקה חייבת להסתכם בו.
        /// <para>
        /// תרגיל רגיל: 100. <b>תרגיל בונוס עובר 100</b>, והמורה היא שקובעת בכמה: היא מזינה
        /// <see cref="BonusValue"/> — כמה נקודות מקבלים על התרגיל מעבר למאה — והתקרה נגזרת
        /// ממנו. כך יש רק כלל אחד לזכור: <i>הרובריקה מסתכמת בתקרה</i>.
        /// </para>
        /// <para>
        /// נגזר ולא נשמר בעמודה משלו בכוונה: שני מקורות אמת לאותו מספר נפרדים זה מזה ברגע
        /// שהמורה עורכת את <see cref="BonusValue"/> ושוכחת לעדכן את השני.
        /// </para>
        /// </summary>
        [NotMapped]
        public int MaxScore => IsBonus
            ? TotalPoints + (int)Math.Round(Math.Max(BonusValue, 0))
            : TotalPoints;

        /// <summary>
        /// הציון שמתחתיו ההגשה נשארת פתוחה להגשה חוזרת, בלי הגבלת מספר ניסיונות.
        /// כדי להשתפר התלמידה חייבת להבין — הטסטים המוסתרים ו-<c>TestVisibility</c> דואגים
        /// לכך שניחוש לפי המשוב לא יעבוד.
        /// </summary>
        public int RetryThreshold { get; set; } = DefaultRetryThreshold;

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
        public void AddTest(string input, string expected, bool isSample = false)
        {
            var list = Tests; // קורא מה-JSON
            list.Add(new TestCase { Input = input, Expected = expected, IsSample = isSample });
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

        [NotMapped]
        public List<ReferenceSolutionFile> ReferenceSolution
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ReferenceSolutionJson))
                    return new List<ReferenceSolutionFile>();

                try
                {
                    return JsonSerializer.Deserialize<List<ReferenceSolutionFile>>(ReferenceSolutionJson)
                           ?? new List<ReferenceSolutionFile>();
                }
                catch
                {
                    // אם יש דאטה מקולקל ב־DB – שלא יפיל את השרת
                    return new List<ReferenceSolutionFile>();
                }
            }
            private set
            {
                ReferenceSolutionJson = JsonSerializer.Serialize(value ?? new List<ReferenceSolutionFile>());
            }
        }

        public void SetReferenceSolution(List<ReferenceSolutionFile>? files)
        {
            // שורות ריקות (שם בלי תוכן) נזרקות כאן ולא בטופס: הן מייצרות קובץ ריק שנשלח
            // ל-Judge0 ומקבלות שגיאת קומפילציה שאין לה שום קשר לפתרון שהמורה כתבה.
            ReferenceSolution = files?
                .Where(f => !string.IsNullOrWhiteSpace(f.Content))
                .ToList() ?? new List<ReferenceSolutionFile>();
        }

        [NotMapped]
        public List<StructuralRule> StructuralRules
        {
            get
            {
                if (string.IsNullOrWhiteSpace(StructuralRulesJson))
                    return new List<StructuralRule>();

                try
                {
                    return JsonSerializer.Deserialize<List<StructuralRule>>(StructuralRulesJson)
                           ?? new List<StructuralRule>();
                }
                catch
                {
                    // אם יש דאטה מקולקל ב־DB – שלא יפיל את השרת
                    return new List<StructuralRule>();
                }
            }
            private set
            {
                StructuralRulesJson = JsonSerializer.Serialize(value ?? new List<StructuralRule>());
            }
        }

        public void SetStructuralRules(List<StructuralRule>? rules)
        {
            StructuralRules = rules ?? new List<StructuralRule>();
        }

        /// <summary>
        /// הדרישות המנוקדות בלבד — אלה שנושאות נקודות ברובריקה.
        /// </summary>
        [NotMapped]
        public IEnumerable<StructuralRule> ScoredRules =>
            StructuralRules.Where(r => r.Severity == RuleSeverity.Scored);
    }
}

       

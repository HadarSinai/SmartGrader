namespace SmartGrader.Application.Dtos.Submissions
{
    public class SubmissionResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }

        // השיעור שמתחת לתרגיל. בלעדיו הלקוח חייב לגרור lessonId ב-queryParam ממסך למסך —
        // ובכל מסלול שלא עובר דרך רשימת התרגילים (למשל "הציונים שלי") הוא פשוט חסר.
        public int LessonId { get; set; }

        public string SourceCode { get; set; } = string.Empty;
        public List<SubmissionFileDto> SourceFiles { get; set; } = new();

        public double? Score { get; set; }

        /// <summary>
        /// פירוק הציון — "בדיקות 64 · דרישות 0 · סה"כ 64". <c>null</c> כשאין ציון (למשל
        /// דרישה חוסמת שלא התקיימה, או הגשה שנבדקה לפני שמנוע הרובריקה נכנס).
        /// </summary>
        public ScoreBreakdownDto? ScoreBreakdown { get; set; }

        public AiFeedbackResultDto? Feedback { get; set; }
        public List<TestCaseResultDto> TestResults { get; set; } = new();

        /// <summary>תוצאות הדרישות המבניות — הדרישה, מה נדרש, מה נמצא, והאם עברה.</summary>
        public List<StructuralRuleResultDto> StructuralResults { get; set; } = new();

        /// <summary>
        /// האם ההגשה פתוחה כרגע להגשה חוזרת. מחושב בשרת ולא בקליינט: הכלל מערב את סף הציון
        /// של התרגיל, נעילת השיעור לתלמידה ואישור חד-פעמי של המורה.
        /// </summary>
        public bool CanResubmit { get; set; }

        /// <summary>מספר הניסיון הנוכחי, החל מ-1. רק האחרון נחשב כציון.</summary>
        public int AttemptNumber { get; set; }

        /// <summary>הניסיונות הקודמים, לציר "ניסיון 1: 40 · ניסיון 2: 78". הנוכחי אינו כאן.</summary>
        public List<SubmissionAttemptDto> Attempts { get; set; } = new();

        /// <summary>
        /// המורה אישרה ניסיון נוסף שטרם נוצל. גובר על סף הציון, ונצרך בהגשה הבאה.
        /// </summary>
        public bool HasUnusedExtraAttempt { get; set; }

        /// <summary>הסיבה שהמורה ציינה לאישור — יומן הביקורת שמחליף "מי השתמשה בקוד".</summary>
        public string? ExtraAttemptReason { get; set; }

        /// <summary>הסיבה שהמורה ציינה לדריסת הציון. <c>null</c> כשהציון מחושב.</summary>
        public string? ScoreOverrideReason { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? AiError { get; set; }
        public string? CompileError { get; set; }

        public DateTime SubmittedAt { get; set; }

        public string? StudentName { get; set; }
        public string? AssignmentName { get; set; }
    }
}

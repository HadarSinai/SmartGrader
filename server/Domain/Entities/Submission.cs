using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmartGrader.Domain.Entities
{
    public enum SubmissionStatus
    {
        PendingAi = 0,
        ProcessingAi = 1,
        Done = 2,
        AiFailed = 3,
        CompilationFailed = 4,
        JudgeUnavailable = 5,

        /// <summary>
        /// דרישה מבנית חוסמת לא התקיימה. <b>אין ציון כלל</b> — לא ציון נמוך: התרגיל דרש
        /// רקורסיה והתלמידה כתבה לולאה, כלומר לא עשתה את מה שהתבקשה. Judge0 אינו נקרא במסלול
        /// הזה בכלל.
        /// </summary>
        RequirementsNotMet = 6
    }

    public class Submission
    {
        /// <summary>
        /// המרווח המזערי בין שתי הגשות של אותה תלמידה לאותו תרגיל.
        /// <para>
        /// אין תקרת ניסיונות, ולכן <c>while(true){}</c> עולה <c>cpu_time_limit × מספר הטסטים</c>
        /// שניות וניתן לשלוח אותו שוב מיד. המרווח הזה גם בולע לחיצה כפולה על "שליחה".
        /// </para>
        /// </summary>
        public static readonly TimeSpan MinResubmitInterval = TimeSpan.FromMinutes(1);

        public int Id { get; private set; }

        public int StudentId { get; private set; }
        public int AssignmentId { get; private set; }

        public string SourceCode { get; private set; } = "";
        public string SourceFilesJson { get; private set; } = "[]";

        public double? Score { get; private set; }
        public string? FeedbackJson { get; private set; }

        public string TestResultsJson { get; private set; } = "[]";

        /// <summary>תוצאות הדרישות המבניות, כ-JSON של <see cref="StructuralRuleResult"/>.</summary>
        public string StructuralResultsJson { get; private set; } = "[]";

        /// <summary>
        /// פירוק הציון (<see cref="ScoreBreakdown"/>) כ-JSON, כדי שהמסך יראה
        /// "בדיקות 64 · דרישות 0" ולא רק מספר אחד. <c>null</c> כשעדיין אין ציון.
        /// </summary>
        public string? ScoreBreakdownJson { get; private set; }

        public SubmissionStatus Status { get; private set; } = SubmissionStatus.PendingAi;
        public string? AiError { get; private set; }
        public string? CompileError { get; private set; }

        public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// מועד הניסיון האחרון. <see cref="SubmittedAt"/> נשאר על ההגשה הראשונה — בלי הפרדה
        /// כזו הגבלת הקצב הייתה משתמשת בתאריך שלא זז אף פעם.
        /// </summary>
        public DateTime LastSubmittedAt { get; private set; } = DateTime.UtcNow;

        /// <summary>מספר הניסיון הנוכחי, החל מ-1. רק האחרון נחשב כציון.</summary>
        public int AttemptNumber { get; private set; } = 1;

        public DateTime? GradedAt { get; private set; }

        // ── אישור הגשה נוספת מהמורה ──
        // כפתור, לא קוד משותף: קוד עובר בין תלמידות ומנטרל את הכלל בשקט, בעוד כפתור כבר
        // מוגן בהזדהות הקיימת. המעקב כאן הוא מה שמחליף את "לראות מי השתמשה בקוד".

        /// <summary>דגל חד-פעמי — נצרך על ידי ההגשה הבאה ומתאפס.</summary>
        public bool HasUnusedExtraAttempt { get; private set; }
        public int? ExtraAttemptGrantedByUserId { get; private set; }
        public DateTime? ExtraAttemptGrantedAt { get; private set; }
        public string? ExtraAttemptReason { get; private set; }

        // ── דריסת ציון ידנית ──
        public int? ScoreOverriddenByUserId { get; private set; }
        public DateTime? ScoreOverriddenAt { get; private set; }
        public string? ScoreOverrideReason { get; private set; }

        public Student Student { get; private set; } = null!;
        public Assignment Assignment { get; private set; } = null!;

        /// <summary>ההיסטוריה של הניסיונות הקודמים. הניסיון הנוכחי אינו כאן — הוא השורה עצמה.</summary>
        public ICollection<SubmissionAttempt> Attempts { get; private set; } = new List<SubmissionAttempt>();

        private Submission() { } // EF Core

        public Submission(int studentId, int assignmentId, string sourceCode, List<SubmissionFile>? sourceFiles = null)
        {
            StudentId = studentId;
            AssignmentId = assignmentId;
            SourceCode = sourceCode;
            SubmittedAt = DateTime.UtcNow;
            SourceFiles = sourceFiles ?? new List<SubmissionFile>();

           // MarkPendingAi();
        }

        [NotMapped]
        public List<TestCaseResult> TestResults
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TestResultsJson))
                    return new List<TestCaseResult>();

                try
                {
                    return JsonSerializer.Deserialize<List<TestCaseResult>>(TestResultsJson)
                           ?? new List<TestCaseResult>();
                }
                catch
                {
                    // אם יש דאטה מקולקל ב־DB – שלא יפיל את השרת
                    return new List<TestCaseResult>();
                }
            }
            private set
            {
                TestResultsJson = JsonSerializer.Serialize(value ?? new List<TestCaseResult>());
            }
        }

        public void SetTestResults(List<TestCaseResult>? results)
        {
            TestResults = results ?? new List<TestCaseResult>();
        }

        [NotMapped]
        public List<SubmissionFile> SourceFiles
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SourceFilesJson))
                    return new List<SubmissionFile>();

                try
                {
                    return JsonSerializer.Deserialize<List<SubmissionFile>>(SourceFilesJson)
                           ?? new List<SubmissionFile>();
                }
                catch
                {
                    // אם יש דאטה מקולקל ב־DB – שלא יפיל את השרת
                    return new List<SubmissionFile>();
                }
            }
            private set
            {
                SourceFilesJson = JsonSerializer.Serialize(value ?? new List<SubmissionFile>());
            }
        }

        public void SetSourceFiles(List<SubmissionFile>? sourceFiles)
        {
            SourceFiles = sourceFiles ?? new List<SubmissionFile>();
        }

        [NotMapped]
        public List<StructuralRuleResult> StructuralResults
        {
            get
            {
                if (string.IsNullOrWhiteSpace(StructuralResultsJson))
                    return new List<StructuralRuleResult>();

                try
                {
                    return JsonSerializer.Deserialize<List<StructuralRuleResult>>(StructuralResultsJson)
                           ?? new List<StructuralRuleResult>();
                }
                catch
                {
                    // אם יש דאטה מקולקל ב־DB – שלא יפיל את השרת
                    return new List<StructuralRuleResult>();
                }
            }
            private set
            {
                StructuralResultsJson = JsonSerializer.Serialize(value ?? new List<StructuralRuleResult>());
            }
        }

        public void SetStructuralResults(List<StructuralRuleResult>? results)
        {
            StructuralResults = results ?? new List<StructuralRuleResult>();
        }

        [NotMapped]
        public ScoreBreakdown? ScoreBreakdown
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ScoreBreakdownJson))
                    return null;

                try
                {
                    return JsonSerializer.Deserialize<ScoreBreakdown>(ScoreBreakdownJson);
                }
                catch
                {
                    return null;
                }
            }
        }

        public void UpdateSourceCode(string sourceCode, List<SubmissionFile>? sourceFiles = null)
        {
            SourceCode = sourceCode;
            if (sourceFiles is not null)
                SourceFiles = sourceFiles;
        }

        /// <summary>
        /// האם מצב ההגשה הנוכחי מאפשר הגשה חוזרת, בהינתן סף הציון של התרגיל.
        /// <para>
        /// כשל (קומפילציה / AI / דרישה חוסמת) פתוח תמיד; הגשה שנבדקה בהצלחה פתוחה רק כל עוד
        /// הציון מתחת לסף. <see cref="SubmissionStatus.JudgeUnavailable"/> אינו כשל של
        /// התלמידה ואין לה מה לתקן — ר' <c>client-student-area-pattern</c>.
        /// </para>
        /// </summary>
        public bool CanResubmit(int retryThreshold) =>
            HasUnusedExtraAttempt
            || Status is SubmissionStatus.AiFailed
                or SubmissionStatus.CompilationFailed
                or SubmissionStatus.JudgeUnavailable
                or SubmissionStatus.RequirementsNotMet
            || (Status == SubmissionStatus.Done && Score.HasValue && Score.Value < retryThreshold);

        /// <summary>האם עברו מספיק זמן מהניסיון הקודם.</summary>
        public bool IsRateLimited(DateTime utcNow) =>
            utcNow - LastSubmittedAt < MinResubmitInterval;

        /// <summary>
        /// פותח את ההגשה לניסיון נוסף: מארכב את הניסיון הנוכחי ומאפס את המצב.
        /// </summary>
        /// <param name="retryThreshold">סף הציון של התרגיל (<see cref="Assignment.RetryThreshold"/>).</param>
        /// <param name="isLocked">
        /// האם השיעור ננעל לתלמידה הזו — <c>LessonResult.IsComplete</c> או כיתה בארכיון.
        /// אישור המורה אינו עוקף נעילה כזו; לשם כך יש פתיחה מחדש של הציון הסופי.
        /// </param>
        /// <remarks>
        /// ⚠️ הכלל נאכף כאן ולא רק ב-handler. זו נקודת הכניסה היחידה למחזור חוזר, ובדיקה
        /// שיושבת רק בשכבה שמעליה נעקפת על ידי כל קורא חדש.
        /// </remarks>
        public void MarkPendingAi(
            int retryThreshold = Assignment.DefaultRetryThreshold,
            bool isLocked = false)
        {
            if (isLocked)
                throw new InvalidOperationException(
                    "Cannot resubmit: the lesson is finalized or the class is archived");

            if (!CanResubmit(retryThreshold))
                throw new InvalidOperationException(
                    $"Cannot move to PendingAi from {Status} with score {Score}");

            if (IsRateLimited(DateTime.UtcNow))
                throw new InvalidOperationException(
                    $"Cannot resubmit before {MinResubmitInterval.TotalMinutes:0} minute(s) have passed");

            // ארכוב לפני האיפוס: בלי זה כל ניסיון קודם נמחק במקום, ועם ניסיונות חופשיים
            // הרשומה פשוט נעלמת.
            ArchiveCurrentAttempt();

            // האישור החד-פעמי נצרך כאן, בין אם הוא זה שפתח את ההגשה ובין אם לא — אחרת הוא
            // נשאר תלוי ופותח גם את הניסיון הבא.
            HasUnusedExtraAttempt = false;

            Status = SubmissionStatus.PendingAi;
            AiError = null;
            CompileError = null;
            Score = null;
            FeedbackJson = null;
            ScoreBreakdownJson = null;
            TestResults = new List<TestCaseResult>();
            StructuralResults = new List<StructuralRuleResult>();

            // GradedAt נשאר תקוע על התאריך הישן והמסכים הציגו "נבדק ב-" של ניסיון קודם.
            GradedAt = null;

            AttemptNumber++;
            LastSubmittedAt = DateTime.UtcNow;
        }

        private void ArchiveCurrentAttempt()
        {
            Attempts.Add(SubmissionAttempt.CaptureFrom(this));

            // שומרים את N האחרונים במלואם וגוזמים את מה שמעבר — ר' SubmissionAttempt.
            var toCollapse = Attempts
                .OrderByDescending(a => a.AttemptNumber)
                .Skip(SubmissionAttempt.FullDetailRetentionCount);

            foreach (var attempt in toCollapse)
                attempt.Collapse();
        }

        // ── פעולות המורה ──────────────────────────────────────────────────────

        /// <summary>
        /// מאשרת לתלמידה ניסיון נוסף, <b>מעל כל כלל</b> — סף ה-85 כלול.
        /// <para>
        /// כפתור ולא קוד משותף: קוד עובר בין תלמידות ומנטרל את הכלל בשקט, בעוד כפתור כבר
        /// מוגן בהזדהות הקיימת. המעקב כאן (מי · מתי · למה) הוא מה שמחליף את "לראות מי
        /// השתמשה בקוד".
        /// </para>
        /// <para>
        /// ⚠️ <b>אינו עוקף נעילת שיעור.</b> שיעור שסוכם לתלמידה או כיתה בארכיון נפתחים
        /// בפתיחה מחדש של הציון הסופי, לא מכאן — אחרת ציון סופי שכבר נמסר היה משתנה מתחתיו.
        /// </para>
        /// </summary>
        public void GrantExtraAttempt(int teacherUserId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required — it is the audit trail", nameof(reason));

            HasUnusedExtraAttempt = true;
            ExtraAttemptGrantedByUserId = teacherUserId;
            ExtraAttemptGrantedAt = DateTime.UtcNow;
            ExtraAttemptReason = reason;
        }

        /// <summary>
        /// דריסת הציון בידי המורה. רשת ביטחון, לא חלק מהמסלול הרגיל.
        /// <para>
        /// עד כה ל-<see cref="Score"/> לא היה שום mutator ציבורי, כך שציון שגוי לא היה ניתן
        /// לתיקון בשום דרך.
        /// </para>
        /// </summary>
        /// <remarks>
        /// התחום הוא תמיד <c>0..100</c>, גם בתרגיל בונוס: הבונוס מוסיף לציון השיעור ואינו
        /// מרים את תקרת התרגיל (<see cref="Assignment.TotalPoints"/>).
        /// </remarks>
        public void OverrideScore(double score, int teacherUserId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required — it is the audit trail", nameof(reason));

            if (score < 0 || score > Assignment.TotalPoints)
                throw new ArgumentOutOfRangeException(
                    nameof(score), $"Score must be between 0 and {Assignment.TotalPoints}");

            Score = score;
            Status = SubmissionStatus.Done;
            GradedAt ??= DateTime.UtcNow;

            ScoreOverriddenByUserId = teacherUserId;
            ScoreOverriddenAt = DateTime.UtcNow;
            ScoreOverrideReason = reason;

            // ⚠️ הפירוק נמחק ולא "מותאם": הוא מתאר חישוב מהרובריקה, וציון שנקבע ידנית לא
            // נובע ממנו. פירוק שנשאר היה מציג "בדיקות 64 · דרישות 0" ליד ציון 90.
            ScoreBreakdownJson = null;
        }

        public void MarkProcessingAi()
        {
            if (Status != SubmissionStatus.PendingAi)
                throw new InvalidOperationException(
                    $"Cannot start AI processing from {Status}");

            Status = SubmissionStatus.ProcessingAi;
            AiError = null;
        }
        /// <summary>
        /// מסיים בדיקה עם ציון. הציון מגיע מ-<c>ScoreCalculator</c> ולעולם לא ממודל שפה.
        /// </summary>
        /// <param name="breakdown">
        /// פירוק הציון, כדי שהמסך יראה "בדיקות 64 · דרישות 0 · סה""כ 64" ולא מספר בודד
        /// שאי אפשר להתווכח איתו.
        /// </param>
        /// <param name="feedbackJson">
        /// המשוב המילולי. <c>null</c> מותר: המשוב הוא תוספת, והציון כבר חושב בלעדיו.
        /// </param>
        public void MarkDone(ScoreBreakdown breakdown, string? feedbackJson)
        {
            if (Status != SubmissionStatus.ProcessingAi)
                throw new InvalidOperationException(
                    $"Cannot mark Done from {Status}");

            Score = breakdown.Total;
            ScoreBreakdownJson = JsonSerializer.Serialize(breakdown);
            FeedbackJson = feedbackJson;
            Status = SubmissionStatus.Done;
            AiError = null;
            GradedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// דרישה מבנית חוסמת לא התקיימה: <b>אין ציון כלל</b>, ו-Judge0 לא נקרא.
        /// <para>
        /// זו אינה הורדת ציון. התרגיל דרש רקורסיה והתלמידה כתבה לולאה — כלומר לא עשתה את
        /// מה שהתבקשה, וההגשה נשארת פתוחה כדי שתגיש שוב.
        /// </para>
        /// </summary>
        public void MarkRequirementsNotMet(string? feedbackJson = null)
        {
            if (Status != SubmissionStatus.PendingAi && Status != SubmissionStatus.ProcessingAi)
                throw new InvalidOperationException(
                    $"Cannot mark RequirementsNotMet from {Status}");

            Status = SubmissionStatus.RequirementsNotMet;

            // מפורש ולא "נשאר כמו שהיה": ציון של ניסיון קודם שנשאר תלוי כאן היה מוצג
            // ליד סטטוס שאומר שאין ציון.
            Score = null;
            ScoreBreakdownJson = null;
            FeedbackJson = feedbackJson;
            AiError = null;
        }

        /// <summary>
        /// מצרף משוב מילולי להגשה שכבר סווגה.
        /// <para>
        /// קיים בשביל שני מסלולי הכשל: שגיאת קומפילציה ודרישה שלא התקיימה נקבעות ונשמרות
        /// <b>לפני</b> הפנייה למודל, כדי שתקלה ב-OpenAI לא תמחק עובדה שכבר הוכרעה.
        /// </para>
        /// </summary>
        public void SetFeedback(string? feedbackJson) => FeedbackJson = feedbackJson;
        public void MarkAiFailed(string error)
        {
            if (Status != SubmissionStatus.ProcessingAi)
                throw new InvalidOperationException(
                    $"Cannot mark AiFailed from {Status}");

            Status = SubmissionStatus.AiFailed;
            AiError = error;
        }

        public void MarkCompilationFailed(string error)
        {
            if (Status != SubmissionStatus.PendingAi && Status != SubmissionStatus.ProcessingAi)
                throw new InvalidOperationException(
                    $"Cannot mark CompilationFailed from {Status}");

            Status = SubmissionStatus.CompilationFailed;
            CompileError = error;
        }

        // כשל תשתית (Judge0 לא זמין / timeout / רשת) — נבדל מכשל קומפילציה ומכשל AI
        public void MarkJudgeUnavailable(string error)
        {
            if (Status != SubmissionStatus.PendingAi && Status != SubmissionStatus.ProcessingAi)
                throw new InvalidOperationException(
                    $"Cannot mark JudgeUnavailable from {Status}");

            Status = SubmissionStatus.JudgeUnavailable;
            AiError = error;
        }

    }
}

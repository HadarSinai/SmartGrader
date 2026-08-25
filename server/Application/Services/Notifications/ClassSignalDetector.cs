using SmartGrader.Application.Dtos.Notifications;
using SmartGrader.Application.Services.CodeAnalysis;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Services.Notifications
{
    /// <summary>
    /// ארבע האגרגציות שמייצרות את הסיגנלים, מתוך אוסף הגשות שכבר נקרא מהמסד.
    /// <para>
    /// ⚠️ <b>קריאת מסד אחת לכל ארבעת הסיגנלים.</b> התוכנית תיארה handler נפרד לכל סיגנל;
    /// כאן זו מתודה נפרדת לכל סיגנל בתוך handler אחד. אותה מודולריות (סיגנל חמישי = מתודה
    /// חמישית + שורת קריאה), בלי לקרוא את אותן ההגשות ארבע פעמים — הפעמון נדגם כל 30 שניות
    /// על ידי כל מורה, ופי ארבע שם אינו זניח.
    /// </para>
    /// <para>
    /// שים לב: <c>Submission</c> הוא שורה אחת בדיוק לכל (תלמידה, תרגיל) — נאכף באינדקס ייחודי —
    /// ולכן ספירת הגשות בקבוצה <i>היא</i> ספירת תלמידות, בלי צורך ב-Distinct.
    /// </para>
    /// </summary>
    public sealed class ClassSignalDetector
    {
        private readonly ClassSignalThresholds _thresholds;

        public ClassSignalDetector(ClassSignalThresholds thresholds)
        {
            _thresholds = thresholds;
        }

        public IReadOnlyList<ClassSignalDto> Detect(IReadOnlyList<Submission> submissions)
        {
            var signals = new List<ClassSignalDto>();

            var groups = submissions
                .Where(s => s.Assignment is not null && s.Assignment.Lesson is not null)
                .GroupBy(s => s.AssignmentId);

            foreach (var group in groups)
            {
                var rows = group.ToList();
                var assignment = rows[0].Assignment;
                var lesson = assignment.Lesson;

                var compilation = DetectCompilationFailure(rows, assignment, lesson);
                if (compilation is not null)
                    signals.Add(compilation);

                // ⚠️ "אף אחת לא עברה" מדוכא כשסיגנל הקומפילציה כבר נורה על אותו תרגיל.
                // שניהם אומרים "התרגיל שבור", אבל סיגנל הקומפילציה כבר נוקב בסיבה; להוסיף
                // לידו "וגם אף אחת לא עברה" זה לספור את אותה תקלה פעמיים בפעמון.
                if (compilation is null)
                {
                    var nobodyPassed = DetectNobodyPassed(rows, assignment, lesson);
                    if (nobodyPassed is not null)
                        signals.Add(nobodyPassed);
                }

                signals.AddRange(DetectStructuralFailures(rows, assignment, lesson));
                signals.AddRange(DetectTestCaseFailures(rows, assignment, lesson));
            }

            // סדר יציב: לפי שיעור, ואז לפי תרגיל, ואז לפי סוג. בלי זה הפעמון מסדר את עצמו
            // מחדש בכל דגימה והמורה חושבת שהגיע משהו חדש.
            return signals
                .OrderBy(s => s.LessonId)
                .ThenBy(s => s.AssignmentId)
                .ThenBy(s => (int)s.Type)
                .ThenBy(s => s.Detail, StringComparer.Ordinal)
                .ToList();
        }

        // ── סיגנל 4: רוב הכיתה לא הצליחה לקמפל ──
        private ClassSignalDto? DetectCompilationFailure(
            IReadOnlyList<Submission> rows, Assignment assignment, Lesson lesson)
        {
            var affected = rows.Count(s => s.Status == SubmissionStatus.CompilationFailed);
            if (!_thresholds.IsMany(affected, rows.Count))
                return null;

            return Build(
                ClassSignalType.CompilationFailedForMost, lesson, assignment, detail: null,
                affected, rows.Count,
                $"{affected} מתוך {rows.Count} מהמגישות לא הצליחו לקמפל את התרגיל \"{Title(assignment)}\" — " +
                "ייתכן שההוראות או שם המתודה אינם מדויקים.");
        }

        // ── סיגנל 3: אף תלמידה לא עברה ──
        private ClassSignalDto? DetectNobodyPassed(
            IReadOnlyList<Submission> rows, Assignment assignment, Lesson lesson)
        {
            if (rows.Count < _thresholds.MinSubmissionsForNobodyPassed)
                return null;

            if (rows.Any(HasPassed))
                return null;

            return Build(
                ClassSignalType.NobodyPassed, lesson, assignment, detail: null,
                rows.Count, rows.Count,
                $"אף אחת מ-{rows.Count} המגישות לא עברה את התרגיל \"{Title(assignment)}\" — " +
                "ייתכן שהפלט הצפוי או חתימת המתודה שגויים.");
        }

        /// <summary>
        /// "עברה" = כל מקרי הליבה עברו. לא ציון מעל סף: תרגיל עם <c>TestsAllocation</c> נמוך
        /// יכול להגיע לציון עובר מנקודות הדרישות בזמן שכל הבדיקות נכשלו, וזה בדיוק המקרה
        /// שהסיגנל הזה נכתב כדי לתפוס.
        /// <para>
        /// כשאין פירוק ציון — הגשה שהמורה דרסה ידנית (<c>OverrideScore</c> מוחק את הפירוק
        /// במכוון) — נופלים חזרה לתוצאות הבדיקות עצמן. ציון שמורה קבעה בעצמה נחשב מעבר.
        /// </para>
        /// </summary>
        private static bool HasPassed(Submission submission) =>
            submission.Status == SubmissionStatus.Done
            && (submission.ScoreBreakdown?.AllCorePassed
                ?? (submission.TestResults.Count == 0 || submission.TestResults.All(r => r.Passed)));

        // ── סיגנל 1: אותה דרישה מבנית נכשלה אצל רבות ──
        private IEnumerable<ClassSignalDto> DetectStructuralFailures(
            IReadOnlyList<Submission> rows, Assignment assignment, Lesson lesson)
        {
            // המפתח הוא הדרישה עצמה ולא מיקומה ברשימה: הדרישות נשמרות על ההגשה בזמן הבדיקה
            // (StructuralResultsJson), והמורה רשאית לערוך את הרובריקה באמצע היום.
            var buckets = new Dictionary<(RuleKind, CodeConstruct, int), Bucket<StructuralRule>>();

            foreach (var submission in rows)
            {
                foreach (var result in submission.StructuralResults)
                {
                    // המלצה שלא התקיימה אינה משנה דבר — לא את הציון ולא את מה שצריך ללמד מחדש.
                    if (result.Rule.Severity == RuleSeverity.Advisory)
                        continue;

                    var key = (result.Rule.Kind, result.Rule.Construct, result.Rule.Threshold);
                    if (!buckets.TryGetValue(key, out var bucket))
                        buckets[key] = bucket = new Bucket<StructuralRule>(result.Rule);

                    bucket.Total++;
                    if (!result.Passed)
                        bucket.Failed++;
                }
            }

            foreach (var bucket in buckets.Values)
            {
                if (!_thresholds.IsMany(bucket.Failed, bucket.Total))
                    continue;

                var text = StructuralRuleDescriber.Describe(bucket.Item);

                yield return Build(
                    ClassSignalType.StructuralRequirementFailed, lesson, assignment, text,
                    bucket.Failed, bucket.Total,
                    $"{bucket.Failed} מתוך {bucket.Total} מהמגישות לא עמדו בדרישה \"{text}\" " +
                    $"בתרגיל \"{Title(assignment)}\".");
            }
        }

        // ── סיגנל 2: אותו מקרה בדיקה נכשל אצל רבות ──
        private IEnumerable<ClassSignalDto> DetectTestCaseFailures(
            IReadOnlyList<Submission> rows, Assignment assignment, Lesson lesson)
        {
            // מקובץ לפי הקלט והפלט הצפוי, לא לפי אינדקס — מאותו נימוק כמו בדרישות המבניות.
            var buckets = new Dictionary<(string, string), Bucket<TestCaseInfo>>();

            foreach (var submission in rows)
            {
                var results = submission.TestResults;
                for (var i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    var key = (result.Input, result.Expected);

                    if (!buckets.TryGetValue(key, out var bucket))
                        buckets[key] = bucket = new Bucket<TestCaseInfo>(
                            new TestCaseInfo(i + 1, result.IsSample, result.Input));
                    else if (i + 1 < bucket.Item.Position)
                        bucket.Item = bucket.Item with { Position = i + 1 };

                    bucket.Total++;
                    if (!result.Passed)
                        bucket.Failed++;
                }
            }

            foreach (var bucket in buckets.Values)
            {
                if (!_thresholds.IsMany(bucket.Failed, bucket.Total))
                    continue;

                // ⚠️ הקלט מצורף רק כשמקרה הבדיקה הוא דוגמה. מקרה מוסתר נושא את התשובה
                // לתרגיל, והמשפט הזה נשלח גם במייל — ומייל אפשר להעביר הלאה. ר' TestVisibility.
                var detail = bucket.Item.IsSample && !string.IsNullOrWhiteSpace(bucket.Item.Input)
                    ? $"בדיקה {bucket.Item.Position} (קלט: {Trim(bucket.Item.Input)})"
                    : $"בדיקה {bucket.Item.Position}";

                yield return Build(
                    ClassSignalType.TestCaseFailed, lesson, assignment, detail,
                    bucket.Failed, bucket.Total,
                    $"{bucket.Failed} מתוך {bucket.Total} מהמגישות נכשלו ב{detail} " +
                    $"בתרגיל \"{Title(assignment)}\" — ייתכן שניסוח התרגיל אינו ברור.");
            }
        }

        private static ClassSignalDto Build(
            ClassSignalType type, Lesson lesson, Assignment assignment, string? detail,
            int affected, int total, string message) =>
            new()
            {
                Key = $"{type}|{assignment.Id}|{detail}",
                Type = type,
                LessonId = lesson.Id,
                LessonSubject = lesson.Subject ?? "",
                AssignmentId = assignment.Id,
                AssignmentTitle = Title(assignment),
                Detail = detail,
                AffectedCount = affected,
                TotalCount = total,
                Message = message
            };

        private static string Title(Assignment assignment) =>
            string.IsNullOrWhiteSpace(assignment.Title) ? $"תרגיל {assignment.Id}" : assignment.Title!;

        private static string Trim(string value) =>
            value.Length <= 30 ? value.Replace("\n", " ") : value[..30].Replace("\n", " ") + "…";

        private sealed class Bucket<T>
        {
            public Bucket(T item) => Item = item;
            public T Item { get; set; }
            public int Failed { get; set; }
            public int Total { get; set; }
        }

        private readonly record struct TestCaseInfo(int Position, bool IsSample, string Input);
    }
}

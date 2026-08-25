using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Services
{
    /// <summary>
    /// הציון הסופי שהמערכת גוזרת מההגשות בשיעור. פונקציה טהורה, באותה רוח כמו
    /// <see cref="ScoreCalculator"/>: אותם קלטים נותנים תמיד אותו מספר.
    /// <para>
    /// 🔴 הפער שזה סוגר: הממוצע חושב עד כה <b>רק בדפדפן</b>. <c>CompleteLessonHandler</c>
    /// קיבל את <c>FinalScore</c> כלשונו מהלקוח וכתב אותו כמו שהוא, כך שהמקום היחיד שבו
    /// הציון הסופי נגזר היה מסך שאפשר לעקוף בבקשת HTTP אחת. הציונים לכל הגשה נקבעים
    /// בשרת ובידי <c>ScoreCalculator</c> — הציון הסופי לא היה.
    /// </para>
    /// <para>
    /// ⚠️ הנוסחה חייבת להישאר זהה למה ש-<c>GetLessonScoreSuggestion</c> מציג למורה, אחרת
    /// הדיאלוג יראה מספר אחד והשרת ידחה אותו כ"חריגה" הדורשת סיבה. לכן שניהם קוראים לכאן.
    /// </para>
    /// </summary>
    public static class LessonScoreCalculator
    {
        public static LessonScoreSummary Calculate(
            IReadOnlyList<Assignment> assignments,
            IReadOnlyList<Submission> submissions)
        {
            assignments ??= Array.Empty<Assignment>();
            submissions ??= Array.Empty<Submission>();

            // הגשה אחת בדיוק לכל (תלמידה, תרגיל), אבל ההצמדה נעשית לפי המאוחרת ליתר ביטחון:
            // האינדקס הייחודי נוסף אחרי שכבר היו נתונים.
            var latestByAssignment = submissions
                .GroupBy(s => s.AssignmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(s => s.SubmittedAt).First());

            var scores = new List<double>();
            var ungraded = 0;

            foreach (var assignment in assignments)
            {
                // ⚠️ תרגיל בלי ציון מדולג ולא נספר כאפס. ממוצע שמכניס 0 על תרגיל שעדיין
                // בבדיקה מציג לתלמידה ציון נמוך שאין לו שום קשר למה שהיא עשתה.
                if (latestByAssignment.TryGetValue(assignment.Id, out var submission)
                    && submission.Score.HasValue)
                    scores.Add(submission.Score.Value);
                else
                    ungraded++;
            }

            return new LessonScoreSummary(
                // מעוגל לספרה אחת, כמו ScoreCalculator: 2 מתוך 3 נותן 66.66666666666666.
                ComputedScore: scores.Count > 0 ? Math.Round(scores.Average(), 1) : null,
                GradedCount: scores.Count,
                UngradedCount: ungraded,
                HasBonus: assignments.Any(a => a.IsBonus));
        }

        /// <summary>
        /// האם הציון שהוזן הוא הציון המחושב. ההשוואה סובלנית לספרה אחת אחרי הנקודה, כי
        /// זה מה ש-<see cref="Calculate"/> מעגל אליו — השוואת <c>==</c> על double הייתה
        /// מסמנת את ההצעה עצמה כחריגה הדורשת סיבה.
        /// </summary>
        public static bool Matches(double? computedScore, double enteredScore) =>
            computedScore.HasValue && Math.Abs(computedScore.Value - enteredScore) < 0.05;
    }

    /// <param name="ComputedScore">
    /// הממוצע על התרגילים שיש להם ציון. <c>null</c> כשאף תרגיל לא נבדק — אין ממה לחשב,
    /// ואז ציון סופי אפשרי רק כדריסה מפורשת ומנומקת.
    /// </param>
    /// <param name="UngradedCount">כמה תרגילים <b>לא</b> נכללו כי אין להם ציון.</param>
    /// <param name="HasBonus">
    /// יש בשיעור תרגיל בונוס, ולכן התקרה היא 150 ולא 100.
    /// ⚠️ נגזר מהתרגילים בפועל ולא ממה שהלקוח שלח.
    /// </param>
    public record LessonScoreSummary(
        double? ComputedScore,
        int GradedCount,
        int UngradedCount,
        bool HasBonus);
}

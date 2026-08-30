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
    /// <b>הבונוס הוא תוספת ברמת השיעור, לא תקרה ברמת התרגיל.</b> כל תרגיל מנוקד מתוך 100,
    /// הבסיס הוא הממוצע על תרגילי <i>החובה</i> שיש להם ציון, וכל תרגיל בונוס מוסיף
    /// <c>BonusValue × (הציון שלה ÷ 100)</c>. התקרה היא <c>100 + Σ BonusValue</c>.
    /// </para>
    /// <para>
    /// ⚠️ המודל הקודם הכניס את הבונוס לממוצע והעמיד תקרה שטוחה של 150: תלמידה שעשתה הכול
    /// בשיעור בן שלושה תרגילים עם בונוס 20 קיבלה 106.7 במקום 120, ואותו בונוס בשיעור בן
    /// עשרה תרגילים היה שווה 2. גודל הבונוס לא אמר את מה שכתוב בו, והתקרה 150 לא נגזרה
    /// משום דבר.
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

            var baseScores = new List<double>();
            var graded = 0;
            var ungraded = 0;
            double bonusPoints = 0;
            double bonusCeiling = 0;
            var hasRequiredAssignment = false;

            foreach (var assignment in assignments)
            {
                // ⚠️ שלילי מנוטרל ולא נסבל: BonusValue שלילי היה מוריד את התקרה מתחת ל-100
                // ומוריד נקודות על תרגיל שהוגדר כתוספת.
                var bonusValue = assignment.IsBonus ? Math.Max(assignment.BonusValue, 0) : 0;

                if (assignment.IsBonus)
                    bonusCeiling += bonusValue;
                else
                    hasRequiredAssignment = true;

                // ⚠️ תרגיל בלי ציון מדולג ולא נספר כאפס. ממוצע שמכניס 0 על תרגיל שעדיין
                // בבדיקה מציג לתלמידה ציון נמוך שאין לו שום קשר למה שהיא עשתה.
                if (!latestByAssignment.TryGetValue(assignment.Id, out var submission)
                    || !submission.Score.HasValue)
                {
                    ungraded++;
                    continue;
                }

                graded++;

                if (assignment.IsBonus)
                    // ⚠️ דילוג על בונוס לעולם אינו עונש: הוא לא נכנס לבסיס ומוסיף 0.
                    // חלקי — 70 מתוך 100 על בונוס של 20 — מוסיף 14, לא הכול ולא כלום.
                    bonusPoints += bonusValue * (submission.Score.Value / Assignment.TotalPoints);
                else
                    baseScores.Add(submission.Score.Value);
            }

            // מעוגל לספרה אחת, כמו ScoreCalculator: 2 מתוך 3 נותן 66.66666666666666.
            // כל חלק מעוגל בנפרד ורק אז נסכם, כדי שהבסיס והתוספת שהדיאלוג מציג יסתכמו
            // בדיוק במספר שהוא מציע — סכום שאינו מסתדר על המסך נראה כמו באג גם כשאינו.
            double? baseScore = baseScores.Count > 0 ? Math.Round(baseScores.Average(), 1) : null;
            var bonusAdded = Math.Round(bonusPoints, 1);

            return new LessonScoreSummary(
                // ⚠️ בלי תרגיל חובה שנבדק אין בסיס, ולכן אין ציון מחושב — גם אם היא עשתה
                // את הבונוס. ציון סופי אז אפשרי רק כדריסה מפורשת ומנומקת.
                ComputedScore: baseScore.HasValue
                    ? Math.Round(baseScore.Value + bonusAdded, 1)
                    : null,
                BaseScore: baseScore,
                BonusPoints: bonusAdded,
                MaxScore: Math.Round(Assignment.TotalPoints + bonusCeiling, 1),
                GradedCount: graded,
                UngradedCount: ungraded,
                HasRequiredAssignment: hasRequiredAssignment);
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
    /// הבסיס ועוד תוספת הבונוס. <c>null</c> כשאף תרגיל חובה לא נבדק — אין ממה לחשב בסיס,
    /// ואז ציון סופי אפשרי רק כדריסה מפורשת ומנומקת.
    /// </param>
    /// <param name="BaseScore">
    /// הממוצע הלא-משוקלל על תרגילי החובה שיש להם ציון. <c>null</c> כשאין אף אחד כזה.
    /// </param>
    /// <param name="BonusPoints">
    /// כמה נקודות הוסיפו תרגילי הבונוס בפועל. 0 כשאין בונוס, וגם כשהוא לא הוגש.
    /// </param>
    /// <param name="MaxScore">
    /// תקרת השיעור: <c>100 + Σ BonusValue</c> על תרגילי הבונוס שבו.
    /// ⚠️ נגזר מהתרגילים בפועל ולא ממה שהלקוח שלח.
    /// </param>
    /// <param name="UngradedCount">כמה תרגילים <b>לא</b> נכללו כי אין להם ציון.</param>
    /// <param name="HasRequiredAssignment">
    /// יש בשיעור לפחות תרגיל אחד שאינו בונוס. שיעור שכולו בונוס אינו יכול לייצר בסיס,
    /// ולכן הוא לעולם לא ייסגר לבד — ר' <c>CompleteLessonHandler</c>.
    /// </param>
    public record LessonScoreSummary(
        double? ComputedScore,
        double? BaseScore,
        double BonusPoints,
        double MaxScore,
        int GradedCount,
        int UngradedCount,
        bool HasRequiredAssignment);
}

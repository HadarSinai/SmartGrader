using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Services
{
    /// <summary>
    /// חישוב הציון מרובריקת 100 הנקודות. פונקציה טהורה — אותם קלטים נותנים תמיד אותו מספר,
    /// וזו כל המטרה: מודל שפה לא נוגע בציון בשום שלב.
    /// <para>
    /// הנוסחה ישבה כביטוי בשורה אחת בתוך <c>AiWorker</c> (<c>passed / total * 100</c>) והיא
    /// עברה לכאן כדי שיהיה מקום אחד שמכיר את הכללים.
    /// </para>
    /// </summary>
    public static class ScoreCalculator
    {
        /// <summary>
        /// מחשב את הציון הסופי ואת פירוקו.
        /// </summary>
        /// <param name="testsAllocation">כמה מהנקודות מוקצות לטסטים. 0 חוקי (תרגיל מחלקות).</param>
        /// <param name="testResults">תוצאות ההרצה. ריק = לא רצה בדיקה כלל, לא "הכל נכשל".</param>
        /// <param name="ruleResults">תוצאות הדרישות המבניות, כולל החוסמות וההמלצות.</param>
        /// <remarks>
        /// ⚠️ להיקרא רק אחרי שכל הדרישות החוסמות עברו. דרישה חוסמת שנכשלה אינה מורידה ציון —
        /// היא מבטלת אותו לגמרי, וזו החלטה של <c>AiWorker</c> לפני שמגיעים לכאן.
        /// <para>
        /// אין כאן פרמטר תקרה, ובכוונה: <b>כל תרגיל מנוקד מתוך 100</b>, בונוס או לא. תקרה
        /// משתנה ברמת התרגיל הייתה מה שהפך בונוס של 20 לשווה 6.7 — הבונוס נמדד עכשיו ברמת
        /// השיעור, ב-<c>LessonScoreCalculator</c>.
        /// </para>
        /// </remarks>
        public static ScoreBreakdown Calculate(
            int testsAllocation,
            IReadOnlyList<TestCaseResult> testResults,
            IReadOnlyList<StructuralRuleResult> ruleResults)
        {
            const int maxScore = Assignment.TotalPoints;

            testResults ??= Array.Empty<TestCaseResult>();
            ruleResults ??= Array.Empty<StructuralRuleResult>();

            var scoredRules = ruleResults
                .Where(r => r.Rule.Severity == RuleSeverity.Scored)
                .ToList();

            int totalTests = testResults.Count;
            int passedTests = testResults.Count(t => t.Passed);

            // ── השער: מקרי ליבה ──
            // ניקוד יחסי טהור מתגמל מזל. הגשה שלא קוראת קלט בכלל ומדפיסה מספר קבוע עוברת
            // במקרה 2 מתוך 5 ומקבלת 32 נקודות על כלום. מנגד, "הכל או כלום" מעניש תלמידה
            // שפתרה נכון ורק שכחה n=0. הפשרה: כל מקרי הליבה הם שער, ומעבר בשער מנקד יחסית
            // על *כל* המקרים, כך שמקרה קצה שנשכח עולה מעט ולא הכול.
            bool allCorePassed = testResults.Where(t => t.IsCore).All(t => t.Passed);

            // total == 0 אינו "נכשל בכול" — פשוט לא היה מה להריץ. ההתנהגות הקודמת נתנה
            // כאן 0 לכל התלמידות בתרגיל בלי טסטים.
            double testPoints = totalTests > 0 && allCorePassed
                ? testsAllocation * (passedTests / (double)totalTests)
                : 0;

            // דרישה היא תנאי, לא מדידה — אין ניקוד חלקי. 4 if במקום 3 מפסיד את כל הנקודות.
            int rulesAllocation = scoredRules.Sum(r => r.Rule.Points);
            double rulePoints = scoredRules.Where(r => r.Passed).Sum(r => r.Rule.Points);

            // ── מקרה הקצה של "הכול שערים" ──
            // תרגיל מחלקות טיפוסי: אפס טסטים ואפס דרישות מנוקדות, רק חוסמות. אין לְמה
            // להקצות נקודות, ולכן 0+0=0 היה נותן אפס לתלמידה שקיימה כל דרישה. הגענו לכאן
            // רק אחרי שכל השערים החוסמים נפתחו — ולכן הציון הוא 100.
            int allocatable = (totalTests > 0 ? testsAllocation : 0) + rulesAllocation;
            if (allocatable == 0)
            {
                return new ScoreBreakdown(
                    TestPoints: 0,
                    RulePoints: 0,
                    Total: maxScore,
                    TestsAllocation: testsAllocation,
                    RulesAllocation: rulesAllocation,
                    PassedTests: passedTests,
                    TotalTests: totalTests,
                    AllCorePassed: allCorePassed);
            }

            // חיתוך בתקרה. הרובריקה אמורה להסתכם בדיוק ב-100, אבל הוולידציה יושבת
            // בשכבה שמעל — וכאן מובטח שגם רובריקה שנשמרה עקומה לא תייצר ציון שאין לו כיסוי.
            var total = Math.Min(testPoints + rulePoints, maxScore);

            // מעוגל לספרה אחת: 2 מתוך 3 נותן 66.66666666666666, והערך הגולמי הזה הוצג
            // כמו שהוא בשישה מסכים שונים.
            return new ScoreBreakdown(
                TestPoints: Math.Round(testPoints, 1),
                RulePoints: Math.Round(rulePoints, 1),
                Total: Math.Round(total, 1),
                TestsAllocation: testsAllocation,
                RulesAllocation: rulesAllocation,
                PassedTests: passedTests,
                TotalTests: totalTests,
                AllCorePassed: allCorePassed);
        }
    }
}

using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Services.CodeRunner;

/// <summary>
/// הבחירה בין שלושת מסלולי ההרצה לפי <see cref="GradingMode"/> — במקום אחד.
/// <para>
/// הלוגיקה הזו ישבה בתוך <c>AiWorker</c> בלבד. ברגע שאימות מקרי הבדיקה של המורה
/// (<c>VerifyTestCases</c>) הריץ קוד גם הוא, הועתקו כאן שני עותקים של אותה בחירה — ואז
/// "בדקתי וזה עבר" של המורה לא היה מבטיח שום דבר על מה שקורה לתלמידה בפועל: העטיפה של
/// מצב <c>Method</c>, כפיית התרבות האינווריאנטית ונרמול הפלט חלים רק על המסלול שנבחר.
/// אימות שרץ במסלול אחר מהניקוד גרוע מאי-אימות, כי הוא נותן ביטחון שקרי.
/// </para>
/// </summary>
public static class GradingModeRunner
{
    /// <param name="sourceFiles">
    /// קבצי המקור. במסלול <see cref="GradingMode.FullProgram"/> ריק מותר — אז
    /// <paramref name="sourceCode"/> נעטף כקובץ יחיד.
    /// </param>
    /// <param name="sourceCode">
    /// הקוד כמחרוזת אחת. זה הקלט של מסלול <see cref="GradingMode.Method"/> (גוף מתודה בודד),
    /// והגיבוי של <see cref="GradingMode.FullProgram"/> כשאין קבצים.
    /// </param>
    public static Task<RunnerResult> RunAsync(
        ICodeRunnerService codeRunner,
        GradingMode gradingMode,
        IReadOnlyList<SubmissionFile> sourceFiles,
        string? sourceCode,
        string methodName,
        IReadOnlyList<ExpectedFile> expectedFiles,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct) => gradingMode switch
        {
            // תוכנית שלמה עם Main של הכותב — בלי עטיפה. אם אין קבצים, עוטפים את SourceCode
            // כ-SubmissionFile יחיד לאותו נתיב מיזוג.
            GradingMode.FullProgram => codeRunner.RunProgramAsync(
                sourceFiles.Count > 0
                    ? sourceFiles
                    : new List<SubmissionFile> { new() { FileName = "Program.cs", Content = sourceCode ?? "" } },
                tests,
                ct),

            GradingMode.MultiFileMethod => codeRunner.RunAsync(
                sourceFiles,
                expectedFiles,
                tests,
                ct),

            // GradingMode.Method וכל ערך אחר (הגנה עתידית). המסלול הזה מקבל מחרוזת אחת;
            // כשהמקור הוא רשימת קבצים הם מחוברים לפני הקריאה — ר' JoinFiles.
            _ => codeRunner.RunAsync(
                !string.IsNullOrWhiteSpace(sourceCode) ? sourceCode : JoinFiles(sourceFiles),
                methodName,
                tests,
                ct),
        };

    /// <summary>
    /// מחבר תוכן קבצים למחרוזת אחת עבור המסלול שמקבל מחרוזת. אין כאן טיפול בשורות using —
    /// <c>Judge0CodeRunner.MergeFiles</c> מרים אותן לראש בעצמו בכל אחד משלושת המסלולים.
    /// </summary>
    private static string JoinFiles(IReadOnlyList<SubmissionFile> files) =>
        string.Join("\n\n", files.Select(f => f.Content));
}

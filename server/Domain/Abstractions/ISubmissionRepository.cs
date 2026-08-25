using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface ISubmissionRepository
    {
        // ⚠️ אין עומס יתר (overload) חסר-teacherId בכוונה — בדיוק דרך החור הזה כל מורה קראה,
        // ערכה ומחקה את ההגשות של תלמידות של מורה אחרת. כל קריאה חייבת להעביר teacherId
        // במפורש (null = מנהל/ת, תלמידה על נתוני עצמה, או קורא מערכת כמו AiWorker).
        Task<IReadOnlyList<Submission>> GetAllAsync(CancellationToken ct = default);
        Task<Submission?> GetByIdAsync(int id, int? teacherId, CancellationToken ct = default);
        Task AddAsync(Submission submission, CancellationToken ct = default);
        Task<IReadOnlyList<Submission>> GetByStudentIdAsync(int studentId, int? teacherId, CancellationToken ct = default);
        Task<IReadOnlyList<Submission>> GetByStudentAndLessonAsync(int studentId, int lessonId, CancellationToken ct = default);

        // ההגשה היחידה של תלמידה לתרגיל. הכלל הוא שורה אחת בדיוק לכל (StudentId, AssignmentId),
        // נאכף גם באינדקס ייחודי ב-DB — ר' CreateSubmissionHandler.
        Task<Submission?> GetByStudentAndAssignmentAsync(int studentId, int assignmentId, CancellationToken ct = default);
        Task<IReadOnlyList<Submission>> GetByLessonIdAsync(int lessonId, CancellationToken ct = default);
        Task<IReadOnlyList<Submission>> GetRecentGradedAsync(int limit, int? teacherId, int? studentId, CancellationToken ct = default);

        /// <summary>
        /// כל ההגשות שהוכרעו בחלון תאריכים — הבסיס לסיגנלים של הפעמון והדיג'סט.
        /// <para>
        /// ⚠️ החלון נחתך לפי <c>LastSubmittedAt</c> ולא לפי <c>GradedAt</c>: <c>GradedAt</c>
        /// נכתב רק ב-<c>MarkDone</c>, כך שהגשה שנכשלה בקומפילציה או שלא עמדה בדרישה חוסמת
        /// מחזיקה <c>NULL</c> — וסינון לפיו היה מוחק בשקט בדיוק את הסיגנלים על תרגיל שבור.
        /// </para>
        /// <para>
        /// "הוכרעה" = Done / CompilationFailed / RequirementsNotMet / AiFailed.
        /// PendingAi ו-ProcessingAi עדיין לא נבדקו, ו-JudgeUnavailable הוא תקלת תשתית ולא
        /// תוצאה של התלמידה — שלושתם היו מרעילים את המכנה.
        /// </para>
        /// </summary>
        Task<IReadOnlyList<Submission>> GetConcludedInRangeAsync(
            DateTime fromUtc, DateTime toUtc, int? teacherId, CancellationToken ct = default);

        // ספירות לשמירה על עבודת תלמידים לפני מחיקה (ר' DeleteLesson/DeleteAssignment/DeleteStudent)
        Task<int> CountByLessonIdAsync(int lessonId, CancellationToken ct = default);
        Task<int> CountByAssignmentIdAsync(int assignmentId, CancellationToken ct = default);
        Task<int> CountByStudentIdAsync(int studentId, CancellationToken ct = default);

        Task DeleteAsync(Submission submission, CancellationToken ct = default);
    }
}

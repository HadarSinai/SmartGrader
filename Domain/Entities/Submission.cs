namespace SmartGrader.Domain.Entities
{
    public class Submission
    {
        public int Id { get; private set; }
        public int StudentId { get; /*private*/ set; }
        public int AssignmentId { get; /*private*/ set; }
        public string SourceCode { get; set; }
        public double Score { get; set; }
        public bool CheckedByAI { get; private set; }
        public string Comments { get; set; }
        public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;

        // קשרים
        public Student Student { get; set; }
        public Assignment Assignment { get; set; }



        //protected Submission() { }

        //public static Submission Create(int studentId, int assignmentId, string sourceCode, string comments = "")
        //{
        //    if (studentId <= 0) throw new ArgumentException("Invalid student id.", nameof(studentId));
        //    if (assignmentId <= 0) throw new ArgumentException("Invalid assignment id.", nameof(assignmentId));
        //    if (string.IsNullOrWhiteSpace(sourceCode)) throw new ArgumentException("SourceCode cannot be empty.", nameof(sourceCode));

        //    return new Submission
        //    {
        //        StudentId = studentId,
        //        AssignmentId = assignmentId,
        //        SourceCode = sourceCode,
        //        Comments = comments,
        //        CheckedByAI = false,
        //        SubmittedAt = DateTime.UtcNow
        //    };
        //}

        //public void MarkCheckedByAI(double score, string comments = "")
        //{
        //    if (CheckedByAI) throw new InvalidOperationException("Already checked by AI.");
        //    if (score < 0 || score > 100) throw new ArgumentOutOfRangeException(nameof(score));

        //    Score = score;
        //    CheckedByAI = true;
        //    if (!string.IsNullOrWhiteSpace(comments))
        //        Comments = comments;
        //}
    }
}

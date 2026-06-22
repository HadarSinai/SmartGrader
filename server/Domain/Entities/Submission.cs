//namespace SmartGrader.Domain.Entities
//{
//    public enum SubmissionStatus
//    {
//        PendingAi = 0,
//        ProcessingAi = 1,
//        Done = 2,
//        AiFailed = 3
//    }

//    public class Submission
//    {
//        public int Id { get; private set; }

//        public int StudentId { get; /*private*/ set; }
//        public int AssignmentId { get; /*private*/ set; }

//        public string SourceCode { get; private set; } = "";

//        public double? Score { get; private set; }
//        public string? Comments { get; private set; }

//        public SubmissionStatus Status { get; private set; } = SubmissionStatus.PendingAi;
//        public string? AiError { get; private set; }

//        public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;

//        // קשרים
//        public Student Student { get; private set; } = null!;
//        public Assignment Assignment { get; private set; } = null!;

//        protected Submission() { }

//        public Submission(int studentId, int assignmentId, string sourceCode)
//        {
//            StudentId = studentId;
//            AssignmentId = assignmentId;
//            SourceCode = sourceCode;
//            SubmittedAt = DateTime.UtcNow;

//            MarkPendingAi();
//        }

//        public void MarkPendingAi()
//        {
//            Status = SubmissionStatus.PendingAi;
//            AiError = null;
//            Score = null;
//            Comments = null;
//        }

//        public void MarkProcessingAi()
//        {
//            Status = SubmissionStatus.ProcessingAi;
//            AiError = null;
//        }

//        public void MarkDone(double score, string comments)
//        {
//            Score = score;
//            Comments = comments;
//            Status = SubmissionStatus.Done;
//            AiError = null;
//        }

//        public void MarkAiFailed(string error)
//        {
//            Status = SubmissionStatus.AiFailed;
//            AiError = error;
//        }
//    }
//}
namespace SmartGrader.Domain.Entities
{
    public enum SubmissionStatus
    {
        PendingAi = 0,
        ProcessingAi = 1,
        Done = 2,
        AiFailed = 3
    }

    public class Submission
    {
        public int Id { get; private set; }

        public int StudentId { get; private set; }
        public int AssignmentId { get; private set; }

        public string SourceCode { get; private set; } = "";

        public double? Score { get; private set; }
        public string? Comments { get; private set; }

        public SubmissionStatus Status { get; private set; } = SubmissionStatus.PendingAi;
        public string? AiError { get; private set; }

        public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;

        public Student Student { get; private set; } = null!;
        public Assignment Assignment { get; private set; } = null!;

        private Submission() { } // EF Core

        public Submission(int studentId, int assignmentId, string sourceCode)
        {
            StudentId = studentId;
            AssignmentId = assignmentId;
            SourceCode = sourceCode;
            SubmittedAt = DateTime.UtcNow;

           // MarkPendingAi();
        }

       
        public void MarkPendingAi()
        {
            if (Status != SubmissionStatus.AiFailed)
                throw new InvalidOperationException(
                    $"Cannot move to PendingAi from {Status}");

            Status = SubmissionStatus.PendingAi;
            AiError = null;
            Score = null;
            Comments = null;
        }

        public void MarkProcessingAi()
        {
            if (Status != SubmissionStatus.PendingAi)
                throw new InvalidOperationException(
                    $"Cannot start AI processing from {Status}");

            Status = SubmissionStatus.ProcessingAi;
            AiError = null;
        }
        public void MarkDone(double score, string comments)
        {
            if (Status != SubmissionStatus.ProcessingAi)
                throw new InvalidOperationException(
                    $"Cannot mark Done from {Status}");

            Score = score;
            Comments = comments ?? "";
            Status = SubmissionStatus.Done;
            AiError = null;
        }
        public void MarkAiFailed(string error)
        {
            if (Status != SubmissionStatus.ProcessingAi)
                throw new InvalidOperationException(
                    $"Cannot mark AiFailed from {Status}");

            Status = SubmissionStatus.AiFailed;
            AiError = error;
        }

    }
}

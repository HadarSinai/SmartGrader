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
        CompilationFailed = 4
    }

    public class Submission
    {
        public int Id { get; private set; }

        public int StudentId { get; private set; }
        public int AssignmentId { get; private set; }

        public string SourceCode { get; private set; } = "";
        public string SourceFilesJson { get; private set; } = "[]";

        public double? Score { get; private set; }
        public string? FeedbackJson { get; private set; }

        public string TestResultsJson { get; private set; } = "[]";

        public SubmissionStatus Status { get; private set; } = SubmissionStatus.PendingAi;
        public string? AiError { get; private set; }
        public string? CompileError { get; private set; }

        public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? GradedAt { get; private set; }

        public Student Student { get; private set; } = null!;
        public Assignment Assignment { get; private set; } = null!;

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

        public void MarkPendingAi()
        {
            if (Status != SubmissionStatus.AiFailed)
                throw new InvalidOperationException(
                    $"Cannot move to PendingAi from {Status}");

            Status = SubmissionStatus.PendingAi;
            AiError = null;
            Score = null;
            FeedbackJson = null;
        }

        public void MarkProcessingAi()
        {
            if (Status != SubmissionStatus.PendingAi)
                throw new InvalidOperationException(
                    $"Cannot start AI processing from {Status}");

            Status = SubmissionStatus.ProcessingAi;
            AiError = null;
        }
        public void MarkDone(double score, string? feedbackJson)
        {
            if (Status != SubmissionStatus.ProcessingAi)
                throw new InvalidOperationException(
                    $"Cannot mark Done from {Status}");

            Score = score;
            FeedbackJson = feedbackJson;
            Status = SubmissionStatus.Done;
            AiError = null;
            GradedAt = DateTime.UtcNow;
        }
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

    }
}

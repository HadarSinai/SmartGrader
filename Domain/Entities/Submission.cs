namespace SmartGrader.Domain.Entities
{
    public class Submission
    {
        public int Id { get; private set; }
        public int StudentId { get; private set; }
        public int AssignmentId { get; private set; }
        public string SourceCode { get; set; }
        public double Score { get; set; }
        public bool CheckedByAI { get; private set; }
        public string Comments { get; set; }
        public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;

        // קשרים
        public Student Student { get; set; }
        public Assignment Assignment { get; set; }
    }
}

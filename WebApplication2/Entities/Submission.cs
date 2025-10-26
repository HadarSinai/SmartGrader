namespace SmartGrader.Entities
{
    public class Submission
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }
        public string SourceCode { get; set; }
        public double Score { get; set; }
        public bool CheckedByAI { get; set; }
        public string Comments { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // קשרים
        public Student Student { get; set; }
        public Assignment Assignment { get; set; }
    }
}

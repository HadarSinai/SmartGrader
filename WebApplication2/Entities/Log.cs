namespace SmartGrader.Entities
{
    public class Log
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }
        public int? LessonId { get; set; }
        public int? AssignmentId { get; set; }
        public string ActionType { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string SystemSource { get; set; }
    }
}


namespace Domain.Entities
{
    public class Log
    {
        public int Id { get; private set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? UserId { get; private set; }
        public int? LessonId { get; private set; }
        public int? AssignmentId { get; private set; }
        public string ActionType { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string SystemSource { get; set; }
    }
}


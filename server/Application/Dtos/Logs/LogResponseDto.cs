namespace SmartGrader.Application.Dtos.Logs
{
    public class LogResponseDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int? UserId { get; set; }
        public int? LessonId { get; set; }
        public int? AssignmentId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SystemSource { get; set; } = string.Empty;
    }
}

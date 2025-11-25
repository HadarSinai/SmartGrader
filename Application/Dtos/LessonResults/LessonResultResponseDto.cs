namespace SmartGrader.Application.Dtos
{
    public class LessonResultResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int LessonId { get; set; }
        public double? FinalScore { get; set; }
        public bool IsComplete { get; set; }
        public DateTime? CalculatedAt { get; set; }
    }
}


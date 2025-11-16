namespace Api.Dtos.LessonResults
{
    public class CompleteLessonRequestDto
    {
        public int StudentId { get; set; }
        public int LessonId { get; set; }
        public double FinalScore { get; set; }
    }
}

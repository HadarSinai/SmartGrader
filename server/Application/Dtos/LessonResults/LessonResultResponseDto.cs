namespace SmartGrader.Application.Dtos
{
    public class LessonResultResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int LessonId { get; set; }
        public double? FinalScore { get; set; }

        /// <summary>מה שהמערכת חישבה. נשמר גם כשהמורה דרסה — ר' LessonResult.ComputedScore.</summary>
        public double? ComputedScore { get; set; }

        public bool IsComplete { get; set; }
        public DateTime? CalculatedAt { get; set; }
        public int TotalAssignments { get; set; }
        public int CompletedAssignments { get; set; }

        // ── יומן הביקורת של דריסת הציון הסופי ──
        public bool IsFinalScoreOverridden { get; set; }
        public int? FinalScoreOverriddenByUserId { get; set; }
        public DateTime? FinalScoreOverriddenAt { get; set; }
        public string? FinalScoreOverrideReason { get; set; }
    }
}


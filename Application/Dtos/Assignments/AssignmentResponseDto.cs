namespace SmartGrader.Application.Dtos.Assignments
{
            public class AssignmentResponseDto
        {
            public int Id { get; set; }
            public int LessonId { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public bool IsBonus { get; set; }
            public double BonusValue { get; set; }
            public DateTime CreatedAt { get; set; }
            public int SubmissionsCount { get; set; }
        }

    }

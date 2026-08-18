namespace SmartGrader.Application.Dtos.LessonResults;

public class StudentGradesSummaryDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<CourseAverageDto> Courses { get; set; } = new();
}

public class CourseAverageDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public double? Average { get; set; }
    public List<StudentGradeItemDto> Grades { get; set; } = new();
}

public class StudentGradeItemDto
{
    public int LessonId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string LessonDateHebrew { get; set; } = string.Empty;
    public double? FinalScore { get; set; }
    public bool IsComplete { get; set; }
}

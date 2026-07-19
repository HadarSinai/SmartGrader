namespace SmartGrader.Application.Dtos.Classes
{
    public class SchoolClassResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AcademicYear { get; set; }
        public string AcademicYearHebrew { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public int StudentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

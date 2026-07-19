namespace SmartGrader.Application.Dtos.Classes
{
    public class CreateClassRequestDto
    {
        public string Name { get; set; } = string.Empty;

        // שנה עברית כמספר (למשל 5786)
        public int AcademicYear { get; set; }
    }
}

namespace SmartGrader.Application.Dtos.Student
{
    public class UpdateStudentRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public int ClassId { get; set; }
    }
}

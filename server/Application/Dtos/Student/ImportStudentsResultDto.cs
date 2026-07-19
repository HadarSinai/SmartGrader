namespace SmartGrader.Application.Dtos.Student
{
    public class ImportStudentsResultDto
    {
        public int CreatedCount { get; set; }
        public List<ImportRowErrorDto> Errors { get; set; } = new();
    }
}

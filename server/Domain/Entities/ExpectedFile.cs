namespace SmartGrader.Domain.Entities
{
    public class ExpectedFile
    {
        public string FileName { get; set; }
        public string? Description { get; set; }
        public string? MethodName { get; set; }
    }
}

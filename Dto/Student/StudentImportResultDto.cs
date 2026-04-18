namespace BE_API.Dto.Student
{
    public class StudentImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}

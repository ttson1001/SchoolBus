namespace BE_API.Dto.User
{
    public class UserImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}

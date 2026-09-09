namespace lmsPortalBe.DTOs.Resource
{
    public class ResourceDto
    {
        public int Id { get; init; } = 0;
        public string CreatorId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime LastEditDate { get; set; }
        public DateTime UploadDate { get; set; }
        public int? CourseId { get; set; }
        public int? ActivityId { get; set; }
        public int? ModuleId { get; set; }
    }
}
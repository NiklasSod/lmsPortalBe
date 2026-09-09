
namespace lmsPortalBe.Models
{
    public class Resource
    {
        public int Id { get; init; } = 0;
        public string CreatorId { get; set; } = string.Empty;
        public ApplicationUser Creator { get; set; } = null!;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime LastEditDate { get; set; }
        public DateTime UploadDate { get; set; }
        public int? CourseId { get; set; }
        public CourseModel? Course { get; set; }
        public int? ActivityId { get; set; } 
        public Activity? Activity { get; set; }
        public int? ModuleId { get; set; }
        public CourseModule? Module { get; set; }
    }
}
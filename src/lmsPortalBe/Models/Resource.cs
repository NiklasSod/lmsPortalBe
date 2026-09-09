
namespace lmsPortalBe.Models
{
    public class Resource
    {
        public int Id { get; init; } = 0;
        public string CreatorId { get; set; } = string.Empty;
        public ApplicationUser Creator { get; set; } = null!;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime? UploadDate { get; set; } = null;
    }
}
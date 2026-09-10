using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Resource
{
    public class CreateResourceRequestDto
    {
        [Required]
        public string DisplayName { get; set; } = string.Empty;
        [Required]
        public string Url { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public int? ActivityId { get; set; }
        public int? ModuleId { get; set; }
    }
}
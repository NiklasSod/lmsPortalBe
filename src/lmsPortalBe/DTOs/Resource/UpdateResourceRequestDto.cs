using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Resource
{
    public class UpdateResourceRequestDto
    {
        public int? CreatorId { get; set; }
        public string? DisplayName { get; set; }
        public string? Url { get; set; }
        public int? CourseId { get; set; }
        public int? ActivityId { get; set; }
        public int? ModuleId { get; set; }
    }
}
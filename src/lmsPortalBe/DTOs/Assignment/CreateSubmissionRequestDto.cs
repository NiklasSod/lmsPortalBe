using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class CreateSubmissionRequestDto
{
  [Required]
  [Range(1, int.MaxValue)]
  public int AssignmentId { get; set; }

  [Required]
  public string Content { get; set; } = string.Empty;
}

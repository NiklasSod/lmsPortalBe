using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class CreateSubmissionRequestDto
{
  [Required]
  public int AssignmentId { get; set; }

  [Required]
  public string Content { get; set; } = string.Empty;
}

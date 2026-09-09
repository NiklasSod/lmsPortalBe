using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class CreateSubmissionRequestDto
{
  [Required]
  public int AssignmentId { get; set; }
  [Required]
  public string StudentId { get; set; } = string.Empty;

  [Required]
  public string Content { get; set; } = string.Empty;

  public string? Feedback { get; set; }

  public DateTime? HandinDate { get; set; } = DateTime.UtcNow;
}

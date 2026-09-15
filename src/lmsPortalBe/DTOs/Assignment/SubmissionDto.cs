using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class SubmissionDto
{
  public int Id { get; set; }
  public int? AssignmentId { get; set; }
  [Required]
  public string StudentId { get; set; } = string.Empty;
  [Required]
  public string Content { get; set; } = string.Empty;

  public string Feedback { get; set; } = string.Empty;

  public DateTime HandinDate { get; set; } = DateTime.UtcNow;
  public DateTime? GradedAt { get; set; }
  [Required]
  public string Status { get; set; } = string.Empty;
}
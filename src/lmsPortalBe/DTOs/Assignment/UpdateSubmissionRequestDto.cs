
namespace lmsPortalBe.DTOs.Course;

public class UpdateSubmissionRequestDto
{
  public int? AssignmentId { get; set; }
  public int? StudentId { get; set; }
  public string? Content { get; set; }
  public string? Feedback { get; set; }
  public DateTime? HandinDate { get; set; }
  public string? Status {get; set; }
}

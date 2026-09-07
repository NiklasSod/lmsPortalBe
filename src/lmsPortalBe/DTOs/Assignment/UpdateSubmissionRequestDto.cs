
namespace lmsPortalBe.DTOs.Course;

public class UpdateSubmissionRequestDto
{
  public int? AssignmentId { get; set; }
  public string? StudentId { get; set; } = string.Empty;
  public string? Content { get; set; }
  public string? Feedback { get; set; }
  // TODO if SubMission is created by teacher and given to all students
  // together with Assignment.., HandInDate should not be set at creation
  public DateTime? HandinDate { get; set; }
  public string? Status { get; set; }
}

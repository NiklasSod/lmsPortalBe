namespace lmsPortalBe.DTOs.Course;

public class StudentAssignmentDto : AssignmentDto
{
  public int? LatestSubmissionId { get; set; }
  public string? LatestSubmissionStatus { get; set; }
  public string LatestFeedback { get; set; } = string.Empty;
}

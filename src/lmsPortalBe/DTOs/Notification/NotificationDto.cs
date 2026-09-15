namespace lmsPortalBe.DTOs.Notification;

public class NotificationDto
{
  public int Id { get; set; }
  public string Type { get; set; } = string.Empty;
  public string Title { get; set; } = string.Empty;
  public string Body { get; set; } = string.Empty;
  public int? CourseId { get; set; }
  public int? ModuleId { get; set; }
  public int? ActivityId { get; set; }
  public int? ResourceId { get; set; }
  public int? SubmissionId { get; set; }
  public DateTime CreatedAt { get; set; }
  public bool IsSeen { get; set; }
  public DateTime? SeenAt { get; set; }
}

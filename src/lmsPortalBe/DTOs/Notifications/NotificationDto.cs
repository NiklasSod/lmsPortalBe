namespace lmsPortalBe.DTOs.Notification
{
  public class NotificationDto
  {
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string? ActorId { get; set; }

    public int? CourseId { get; set; }
    public int? ModuleId { get; set; }
    public int? ActivityId { get; set; }
    public int? ResourceId { get; set; }
    public int? SubmissionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  }
}

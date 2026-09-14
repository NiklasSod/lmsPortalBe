namespace lmsPortalBe.DTOs.Notification
{
  public class UserNotificationDto
  {
    public string UserId { get; set; } = string.Empty;
    public int NotificationId { get; set; }
    public bool IsSeen { get; set; }
    public DateTime? SeenAt { get; set; }
  }
}

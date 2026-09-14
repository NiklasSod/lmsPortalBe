using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Notification
{
  public class UpdateUserNotificationRequestDto
  {
    [Required]
    public bool IsSeen { get; set; }
  }
}

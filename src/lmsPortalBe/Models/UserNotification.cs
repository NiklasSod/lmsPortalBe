namespace lmsPortalBe.Models
{
    public class UserNotification
    {
        public int Id { get; init; } = 0;
        public string UserId { get; set; } = string.Empty;
        public int NotificationId { get; set; }
        public bool IsSeen { get; set; }
        public DateTime? SeenAt { get; set; }

        public ApplicationUser User { get; set; } = null!;
        public Notification Notification { get; set; } = null!;
    }
}

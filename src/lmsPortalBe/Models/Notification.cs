namespace lmsPortalBe.Models
{
    public class Notification
    {
        public int Id { get; init; } = 0;
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public string? ActorId { get; set; }
        public ApplicationUser? Actor { get; set; }

        public int? CourseId { get; set; }
        public int? ModuleId { get; set; }
        public int? ActivityId { get; set; }
        public int? ResourceId { get; set; }
        public int? SubmissionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserNotification> Recipients { get; set; } = [];
    }
}

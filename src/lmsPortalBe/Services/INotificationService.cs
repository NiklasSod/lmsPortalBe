using lmsPortalBe.Models;

namespace lmsPortalBe.Services
{
  public interface INotificationService
  {
    /// <summary>
    /// Creates one notification and fans it out to every student
    /// enrolled in the given course.
    /// </summary>
    Task NotifyCourseStudentsAsync(
        int courseId,
        NotificationType type,
        string title,
        string body,
        string actorId,
        int? moduleId = null,
        int? activityId = null,
        int? resourceId = null,
        CancellationToken cancellationToken = default);
  }
}

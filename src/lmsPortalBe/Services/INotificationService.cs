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

    /// <summary>
    /// Creates one notification for a single recipient (e.g. a student
    /// whose submission was graded).
    /// </summary>
    Task NotifyStudentAsync(
        string userId,
        NotificationType type,
        string title,
        string body,
        string actorId,
        int? courseId = null,
        int? submissionId = null,
        CancellationToken cancellationToken = default);
  }
}

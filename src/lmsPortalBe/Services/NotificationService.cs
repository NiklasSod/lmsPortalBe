using lmsPortalBe.Data;
using lmsPortalBe.Models;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Services
{
  public class NotificationService(ILmsPortalContext context) : INotificationService
  {
    private readonly ILmsPortalContext _context = context;

    public async Task NotifyCourseStudentsAsync(
        int courseId,
        NotificationType type,
        string title,
        string body,
        string actorId,
        int? moduleId = null,
        int? activityId = null,
        int? resourceId = null,
        CancellationToken cancellationToken = default)
    {
      var studentIds = await _context.CourseEnrollments
          .Where(e => e.CourseId == courseId && e.Role == CourseRole.Student)
          .Select(e => e.UserId)
          .ToListAsync(cancellationToken);

      if (studentIds.Count == 0)
      {
        return;
      }

      var notification = new Notification
      {
        Type = type,
        Title = title,
        Body = body,
        ActorId = actorId,
        CourseId = courseId,
        ModuleId = moduleId,
        ActivityId = activityId,
        ResourceId = resourceId,
        CreatedAt = DateTime.UtcNow
      };

      _context.Notifications.Add(notification);

      var recipients = studentIds
          .Select(studentId => new UserNotification
          {
            UserId = studentId,
            Notification = notification
          })
          .ToList();

      _context.UserNotifications.AddRange(recipients);

      await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyStudentAsync(
        string userId,
        NotificationType type,
        string title,
        string body,
        string actorId,
        int? courseId = null,
        int? submissionId = null,
        CancellationToken cancellationToken = default)
    {
      var notification = new Notification
      {
        Type = type,
        Title = title,
        Body = body,
        ActorId = actorId,
        CourseId = courseId,
        SubmissionId = submissionId,
        CreatedAt = DateTime.UtcNow
      };

      _context.Notifications.Add(notification);
      _context.UserNotifications.Add(new UserNotification
      {
        UserId = userId,
        Notification = notification
      });

      await _context.SaveChangesAsync(cancellationToken);
    }
  }
}

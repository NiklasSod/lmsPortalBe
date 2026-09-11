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
        ModuleId = moduleId,
        ActivityId = activityId,
        ResourceId = resourceId,
        CreatedAt = DateTime.UtcNow
      };

      _context.Notifications.Add(notification);

      foreach (var studentId in studentIds)
      {
        _context.UserNotifications.Add(new UserNotification
        {
          UserId = studentId,
          Notification = notification
        });
      }

      await _context.SaveChangesAsync(cancellationToken);
    }
  }
}

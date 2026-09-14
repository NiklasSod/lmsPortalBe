using AutoMapper;
using lmsPortalBe.Data;
using lmsPortalBe.DTOs.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Controllers
{
  [Route("api/[controller]")]
  public class NotificationsController(
      ILmsPortalContext context,
      IMapper mapper)
      : CoursePortalControllerBase(context, mapper)
  {
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false)
    {
      var query = _context.UserNotifications
          .Where(un => un.UserId == CurrentUserId);

      if (unreadOnly)
      {
        query = query.Where(un => !un.IsSeen);
      }

      var notifications = await query
          .Include(un => un.Notification)
          .OrderByDescending(un => un.Notification.CreatedAt)
          .ThenByDescending(un => un.NotificationId)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<NotificationDto>));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
      var count = await _context.UserNotifications
          .CountAsync(un => un.UserId == CurrentUserId && !un.IsSeen);

      return Ok(new { Count = count });
    }

    [HttpPost("{id:int}/seen")]
    public async Task<IActionResult> MarkSeen(int id)
    {
      var userNotification = await _context.UserNotifications
          .FirstOrDefaultAsync(un => un.Id == id && un.UserId == CurrentUserId);

      if (userNotification is null)
      {
        return NotFound();
      }

      userNotification.IsSeen = true;
      userNotification.SeenAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();

      return NoContent();
    }

    [HttpPost("seen-all")]
    public async Task<IActionResult> MarkAllSeen()
    {
      var notifications = await _context.UserNotifications
          .Where(un => un.UserId == CurrentUserId && !un.IsSeen)
          .ToListAsync();

      foreach (var notification in notifications)
      {
        notification.IsSeen = true;
        notification.SeenAt = DateTime.UtcNow;
      }

      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}

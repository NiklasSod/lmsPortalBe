using AutoMapper;
using lmsPortalBe.Data;
using lmsPortalBe.DTOs.Course;
using lmsPortalBe.DTOs.Notification;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
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
    private bool UserIsRecipient(Notification notification)
    {
      return notification.Recipients.FirstOrDefault(r => r.UserId == CurrentUserId) is not null;
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllNotifications()
    {
      var notifications = await _context.Notifications
          .OrderBy(a => a.CreatedAt)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<NotificationDto>));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyNotifications()
    {

      var notifications = await _context.Notifications
          .Where(n => n.ActorId == CurrentUserId)
          .OrderBy(n => n.CreatedAt)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<NotificationDto>));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetNotification(int id)
    {
      var notification = await _context.Notifications
          .FirstOrDefaultAsync(n => n.Id == id);
      if (notification is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !UserIsRecipient(notification))
      {
        return Forbid();
      }

      return Ok(_mapper.Map<NotificationDto>(notification));
    }

    [HttpGet("/api/usernotifications")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllUserNotifications()
    {
      var notifications = await _context.UserNotifications
          .OrderBy(a => a.SeenAt)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<UserNotificationDto>));
    }

    [HttpGet("/api/usernotifications/mine")]
    public async Task<IActionResult> GetMyUserNotifications()
    {

      var notifications = await _context.UserNotifications
          .Where(n => n.UserId == CurrentUserId)
          .OrderBy(n => n.SeenAt)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<UserNotificationDto>));
    }

    [HttpGet("/api/usernotifications/{id:int}")]
    public async Task<IActionResult> GetUserNotification(int id)
    {
      var notification = await _context.UserNotifications
          .FirstOrDefaultAsync(n => n.Id == id);
      if (notification is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && notification.UserId != CurrentUserId)
      {
        return Forbid();
      }

      return Ok(_mapper.Map<UserNotificationDto>(notification));
    }

    

    [HttpGet("/api/modules/{id:int}/notifications")]
    public async Task<IActionResult> GetModuleNotifications(int id)
    {
      var module = await _context.CourseModules
          .FirstOrDefaultAsync(c => c.Id == id);
      if (module is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsEnrolledAsync(module.CourseId))
      {
        return Forbid();
      }

      var notifications = await _context.Notifications
          .Where(a => a.ModuleId == id)
          .OrderBy(a => a.CreatedAt)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<NotificationDto>));
    }

    [HttpGet("/api/activities/{id:int}/notifications")]
    public async Task<IActionResult> GetActivityNotifications(int id)
    {
      var activity = await _context.Activities
          .FirstOrDefaultAsync(a => a.Id == id);
      if (activity is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsEnrolledAsync(activity.Module.CourseId))
      {
        return Forbid();
      }

      var notifications = await _context.Notifications
          .Where(a => a.ActivityId == id)
          .OrderBy(a => a.CreatedAt)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<NotificationDto>));
    }

    [HttpGet("/api/courses/{id:int}/notifications")]
    public async Task<IActionResult> GetCourseNotifications(int id)
    {
      var course = await _context.Courses
          .FirstOrDefaultAsync(a => a.Id == id);
      if (course is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsEnrolledAsync(course.Id))
      {
        return Forbid();
      }

      var notifications = await _context.Notifications
          .Where(a => a.CourseId == id)
          .OrderBy(a => a.CreatedAt)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<NotificationDto>));
    }

    [HttpGet("/api/submissions/{id:int}/notifications")]
    public async Task<IActionResult> GetSubmissionNotifications(int id)
    {
      var submission = await _context.Submissions
          .FirstOrDefaultAsync(a => a.Id == id);
      if (submission is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && submission.StudentId != CurrentUserId)
      {
        return Forbid();
      }

      var notifications = await _context.Notifications
          .Where(a => a.SubmissionId == id)
          .OrderBy(a => a.CreatedAt)
          .ToListAsync();

      return Ok(notifications.Select(_mapper.Map<NotificationDto>));
    }

    [HttpPatch("/api/usernotifications/{id:int}")]
    public async Task<IActionResult> UpdateUserNotification(int id, UpdateUserNotificationRequestDto dto)
    {
      var notification = await _context.UserNotifications.FirstOrDefaultAsync(a => a.Id == id);
      if (notification is null)
      {
        return NotFound();
      }

      if (dto.IsSeen)
      {
        notification.IsSeen = true;
        notification.SeenAt ??= DateTime.UtcNow;
      }
      else
      {
        BadRequest("Cannot mark a notification as unseen.");
      }

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<UserNotificationDto>(notification));
    }

    

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
      var notification = await _context.Notifications
          .FirstOrDefaultAsync(c => c.Id == id);
      if (notification is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && notification.ActorId != CurrentUserId)
      {
        return Forbid();
      }

      _context.Notifications.Remove(notification);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    [HttpDelete("/api/usernotifications/{id:int}")]
    public async Task<IActionResult> DeleteUserNotification(int id)
    {
      var notification = await _context.UserNotifications
          .FirstOrDefaultAsync(c => c.Id == id);
      if (notification is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && notification.UserId != CurrentUserId)
      {
        return Forbid();
      }

      _context.UserNotifications.Remove(notification);
      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}

using System.Security.Claims;
using AutoMapper;
using lmsPortalBe.Data;
using lmsPortalBe.DTOs.Course;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  [Authorize]
  public class ActivitiesController(
      ILmsPortalContext context,
      IMapper mapper) : ControllerBase
  {
    private readonly ILmsPortalContext _context = context;
    private readonly IMapper _mapper = mapper;

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");

    [HttpGet]
    public async Task<IActionResult> GetAllActivities()
    {
      var courses = await _context.Activities
          .OrderBy(a => a.StartDate)
          .ToListAsync();

      return Ok(courses.Select(_mapper.Map<ActivityDto>));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetUserActivities()
    {
      var enrolledCourses = await _context.CourseEnrollments
        .Where(e => e.UserId == CurrentUserId)
        .Select(e => e.CourseId)
        .ToListAsync();

      var activities = await _context.CourseModules
          .Where(m => enrolledCourses.Contains(m.CourseId))
          .SelectMany(m => m.Activities)
          .OrderBy(a => a.StartDate)
          .ToListAsync();
      

      return Ok(activities.Select(_mapper.Map<ActivityDto>));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetActivity(int id)
    {
      var activity = await _context.Activities
          .FirstOrDefaultAsync(c => c.Id == id);
      if (activity is null)
      {
        return NotFound();
      }

      return Ok(_mapper.Map<ActivityDto>(activity));
    }

    [HttpGet("/api/modules/{id:int}/activities")]
    public async Task<IActionResult> GetModuleActivities(int id)
    {
      var module = await _context.CourseModules
          .FirstOrDefaultAsync(c => c.Id == id);
      if (module is null)
      {
        return NotFound();
      }
      
      var activities = await _context.Activities
          .Where(a => a.ModuleId == id)
          .OrderBy(a => a.StartDate)
          .ToListAsync();

      return Ok(activities.Select(_mapper.Map<ActivityDto>));
    }

    [HttpPost("api/activities")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> CreateActivity(CreateActivityRequestDto dto)
    {

      if (dto.EndDate <= dto.StartDate)
      {
        return BadRequest("The activity seem to end before it starts, check the start and end dates.");
      }

      var module = await _context.CourseModules.FirstOrDefaultAsync(c => c.Id == dto.ModuleId);
      if (module is null)
      {
        return NotFound("Cannot find module to add activity to.");
      }

      if (dto.EndDate < module.StartDate || dto.EndDate > module.EndDate ||
          dto.StartDate < module.StartDate || dto.StartDate > module.EndDate)
      {
        return BadRequest("The activity seem to extend outside the module's timeframe, check the start and end dates.");
      }

      if (!Enum.TryParse(typeof(ActivityType), dto.Type, out object? result))
      {
        return BadRequest("Cannot recognize activity type.");
      }

      var activity = new Activity
      {
        ModuleId = dto.ModuleId,
        Name = dto.Name,
        ActivityType = (ActivityType) result,
        Description = dto.Description,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate
      };

      _context.Activities.Add(activity);
      module.Activities.Add(activity);

      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetActivity), new { id = activity.Id }, _mapper.Map<ActivityDto>(activity));
    }


    [HttpPost("api/modules/{moduleId:int}/activities")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> CreateActivityInModule(int moduleId, CreateActivityRequestDto dto)
    {
      if (dto.ModuleId != moduleId)
      {
        return BadRequest("Module Id in request body does not match id in route.");
      }
      return await CreateActivity(dto);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> UpdateActivity(int id, UpdateActivityRequestDto dto)
    {
      var activity = await _context.Activities.FirstOrDefaultAsync(c => c.Id == id);
      if (activity is null)
      {
        return NotFound();
      }

      if (dto.EndDate <= dto.StartDate)
      {
        return BadRequest("Activity can't end before it starts, check the start and end dates.");
      }

      var module = await _context.CourseModules.FirstOrDefaultAsync(c => c.Id == dto.ModuleId);
      if (module is null)
      {
        return NotFound("Cannot find module to add activity to.");
      }

      if (dto.EndDate < module.StartDate || dto.EndDate > module.EndDate ||
          dto.StartDate < module.StartDate || dto.StartDate > module.EndDate)
      {
        return BadRequest("Activity can't extend outside the module's timeframe, check the start and end dates.");
      }

      var isTeacherOfCourse = await _context.CourseEnrollments
          .AnyAsync(e => e.CourseId == module.CourseId
              && e.UserId == CurrentUserId
              && e.Role == CourseRole.Teacher);

      if (!User.IsInRole("admin") && !isTeacherOfCourse)
      {
        return Forbid();
      }

      var startDate = dto.StartDate ?? activity.StartDate;
      var endDate = dto.EndDate ?? activity.EndDate;

      if (Enum.TryParse(typeof(ActivityType), dto.Type, out object? result))
      {
        activity.ActivityType = (ActivityType) result;
      }

      if (dto.Name is not null)
      {
        activity.Name = dto.Name;
      }

      if (dto.Description is not null)
      {
        activity.Description = dto.Description;
      }

      activity.StartDate = startDate;
      activity.EndDate = endDate;

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<ActivityDto>(activity));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteActivity(int id)
    {
      var activity = await _context.Activities.FirstOrDefaultAsync(c => c.Id == id);
      if (activity is null)
      {
        return NotFound();
      }

      var isCreator = await _context.CourseEnrollments
          .AnyAsync(e => e.CourseId == activity.Module.CourseId
              && e.UserId == CurrentUserId
              && e.Role == CourseRole.Teacher);

      if (!isCreator)
      {
        return Forbid();
      }

      _context.Activities.Remove(activity);
      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}


using System.Data;
using AutoMapper;
using lmsPortalBe.Data;
using lmsPortalBe.DTOs.Course;
using lmsPortalBe.DTOs.Resource;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Controllers
{

  [Route("api/[controller]")]
  public class ResourcesController(
      ILmsPortalContext context,
      IMapper mapper,
      UserManager<ApplicationUser> _userManager)
      : CoursePortalControllerBase(context, mapper)
  {

    private int GetResourceCourseId(Resource resource) =>
      resource.CourseId
        ?? resource.Activity?.Module?.CourseId
        ?? resource.Module?.CourseId
        ?? throw new KeyNotFoundException("Resource has no parent course.");

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllResources()
    {
      var resources = await _context.Resources
          .OrderBy(r => r.UploadDate)
          .ToListAsync();

      return Ok(resources.Select(_mapper.Map<ResourceDto>));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetUserResources()
    {
      var enrolledCourseIds = await _context.CourseEnrollments
          .Where(e => e.UserId == CurrentUserId)
          .Select(e => e.CourseId)
          .ToListAsync();

      var resources = await _context.Resources
          .Where(r =>
              (r.CourseId != null && enrolledCourseIds.Contains(r.CourseId.Value))
              || (r.Module != null && enrolledCourseIds.Contains(r.Module.CourseId))
              || (r.Activity != null && enrolledCourseIds.Contains(r.Activity.Module.CourseId)))
          .OrderBy(r => r.UploadDate)
          .ToListAsync();

      return Ok(resources.Select(_mapper.Map<ResourceDto>));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetResource(int id)
    {
      var resource = await _context.Resources
          .Include(r => r.Activity)
              .ThenInclude(a => a!.Module)
          .Include(r => r.Module)
          .FirstOrDefaultAsync(r => r.Id == id);

      if (resource is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin")
          && resource.CreatorId != CurrentUserId
          && !await IsEnrolledAsync(GetResourceCourseId(resource)))
      {
        return Forbid();
      }

      return Ok(_mapper.Map<ResourceDto>(resource));
    }

    [HttpGet("/api/courses/{courseId:int}/resources")]
    public async Task<IActionResult> GetCourseResources(int courseId)
    {
      var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
      if (course is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsEnrolledAsync(courseId))
      {
        return Forbid();
      }

      var resources = await _context.Resources
          .Where(r => r.CourseId == courseId)
          .OrderBy(r => r.UploadDate)
          .ToListAsync();

      return Ok(resources.Select(_mapper.Map<ResourceDto>));
    }

    [HttpGet("/api/modules/{moduleId:int}/resources")]
    public async Task<IActionResult> GetModuleResources(int moduleId)
    {
      var module = await _context.CourseModules.FirstOrDefaultAsync(m => m.Id == moduleId);
      if (module is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsEnrolledAsync(module.CourseId))
      {
        return Forbid();
      }

      var resources = await _context.Resources
          .Where(r => r.ModuleId == moduleId)
          .OrderBy(r => r.UploadDate)
          .ToListAsync();

      return Ok(resources.Select(_mapper.Map<ResourceDto>));
    }

    [HttpGet("/api/activity/{activityId:int}/resources")]
    public async Task<IActionResult> GetActivityResources(int activityId)
    {
      var activity = await _context.Activities
          .Include(a => a.Module)
          .FirstOrDefaultAsync(a => a.Id == activityId);
      if (activity is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsEnrolledAsync(activity.Module.CourseId))
      {
        return Forbid();
      }

      var resources = await _context.Resources
          .Where(r => r.ActivityId == activityId)
          .OrderBy(r => r.UploadDate)
          .ToListAsync();

      return Ok(resources.Select(_mapper.Map<ResourceDto>));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateResource(CreateResourceRequestDto dto)
    {
      var user = await _userManager.FindByIdAsync(CurrentUserId);
      if (user is null)
      {
        return NotFound("User not found.");
      }

      var locationCount = (dto.ActivityId is not null ? 1 : 0)
          + (dto.CourseId is not null ? 1 : 0)
          + (dto.ModuleId is not null ? 1 : 0);

      if (locationCount != 1)
      {
        return BadRequest("Must be assigned to exactly one activity, course, or module.");
      }

      var activity = dto.ActivityId is not null ?
        await _context.Activities
            .Include(a => a.Module)
            .FirstOrDefaultAsync(a => a.Id == dto.ActivityId)
        : null;

      var course = dto.CourseId is not null ?
        await _context.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId)
        : null;

      var module = dto.ModuleId is not null ?
        await _context.CourseModules.FirstOrDefaultAsync(m => m.Id == dto.ModuleId)
        : null;

      var resource = new Resource
      {
        DisplayName = dto.DisplayName,
        CreatorId = CurrentUserId,
        Url = dto.Url,
        ActivityId = dto.ActivityId,
        CourseId = dto.CourseId,
        ModuleId = dto.ModuleId,
        Activity = activity,
        Course = course,
        Module = module,
        UploadDate = DateTime.UtcNow,
        LastEditDate = DateTime.UtcNow,
      };

      if (!User.IsInRole("admin")
          && !await IsCourseTeacherAsync(GetResourceCourseId(resource)))
      {
        return Forbid();
      }

      _context.Resources.Add(resource);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, _mapper.Map<ResourceDto>(resource));
    }

    [HttpPost("/api/activity/{activityId:int}/resources")]
    [Authorize]
    public async Task<IActionResult> CreateResourceForActivity(int activityId, CreateResourceRequestDto dto)
    {
      dto.ActivityId = activityId;
      return await CreateResource(dto);
    }

    [HttpPost("/api/courses/{courseId:int}/resources")]
    [Authorize]
    public async Task<IActionResult> CreateResourceForCourse(int courseId, CreateResourceRequestDto dto)
    {
      dto.CourseId = courseId;
      return await CreateResource(dto);
    }

    [HttpPost("/api/modules/{moduleId:int}/resources")]
    [Authorize]
    public async Task<IActionResult> CreateResourceForModule(int moduleId, CreateResourceRequestDto dto)
    {
      dto.ModuleId = moduleId;
      return await CreateResource(dto);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> UpdateResource(int id, UpdateResourceRequestDto dto)
    {
      var resource = await _context.Resources.FirstOrDefaultAsync(c => c.Id == id);
      if (resource is null)
      {
        return NotFound();
      }


      if (!User.IsInRole("admin")
          && resource.CreatorId != CurrentUserId)
      {
        return Forbid();
      }

      var hasLocationChange = dto.ActivityId is not null
          || dto.CourseId is not null
          || dto.ModuleId is not null;

      if (!User.IsInRole("admin") && hasLocationChange)
      {
        return Forbid("Only admin may move resource to new location.");
      }

      if (hasLocationChange)
      {
        var locationCount = (dto.ActivityId is not null ? 1 : 0)
            + (dto.CourseId is not null ? 1 : 0)
            + (dto.ModuleId is not null ? 1 : 0);

        if (locationCount != 1)
        {
          return BadRequest("Can only belong to a course, module, or activity.");
        }
      }

      if (dto.ActivityId is not null)
      {
        var newActivity = await _context.Activities.FirstOrDefaultAsync(a => a.Id == dto.ActivityId);
        if (newActivity is not null)
        {
          resource.Activity = newActivity;
          resource.ActivityId = dto.ActivityId;
          resource.Course = null;
          resource.CourseId = null;
          resource.Module = null;
          resource.ModuleId = null;
        }
      }

      if (dto.CourseId is not null)
      {
        var newCourse = await _context.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
        if (newCourse is not null)
        {
          resource.Course = newCourse;
          resource.CourseId = dto.CourseId;
          resource.Activity = null;
          resource.ActivityId = null;
          resource.Module = null;
          resource.ModuleId = null;
        }
      }

      if (dto.ModuleId is not null)
      {
        var newModule = await _context.CourseModules.FirstOrDefaultAsync(m => m.Id == dto.ModuleId);
        if (newModule is not null)
        {
          resource.Module = newModule;
          resource.ModuleId = dto.ModuleId;
          resource.Activity = null;
          resource.ActivityId = null;
          resource.Course = null;
          resource.CourseId = null;
        }
      }

      resource.Url = dto.Url ?? resource.Url;
      resource.DisplayName = dto.DisplayName ?? resource.DisplayName;
      resource.LastEditDate = DateTime.UtcNow;

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<ResourceDto>(resource));
    }



    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteResource(int id)
    {
      var resource = await _context.Resources.FirstOrDefaultAsync(c => c.Id == id);
      if (resource is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin")
          && resource.CreatorId != CurrentUserId)
      {
        return Forbid();
      }

      _context.Resources.Remove(resource);
      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}

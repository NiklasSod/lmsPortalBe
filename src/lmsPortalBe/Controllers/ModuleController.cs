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
  [Authorize(Roles = "student,teacher")]
  public class ModulesController(
      ILmsPortalContext context,
      IMapper mapper) : ControllerBase
  {
    private readonly ILmsPortalContext _context = context;
    private readonly IMapper _mapper = mapper;

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");

    [HttpGet]
    public async Task<IActionResult> GetAllModules()
    {
      var modules = await _context.CourseModules
          .OrderBy(c => c.StartDate)
          .ToListAsync();

      return Ok(modules.Select(_mapper.Map<CourseModuleSummaryDto>));
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetUserCurrentModules()
    {
      var enrolledCourses = await _context.CourseEnrollments
        .Where(e => e.UserId == CurrentUserId)
        .Select(e => e.CourseId)
        .ToListAsync();

      var modules = await _context.CourseModules
          .Where(m => enrolledCourses.Contains(m.CourseId))
          .Where(m => m.EndDate > DateTime.Now && m.StartDate <= DateTime.Now)
          .OrderBy(m => m.StartDate)
          .ToListAsync();

      return Ok(modules.Select(_mapper.Map<CourseModuleSummaryDto>));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetModule(int id)
    {
      var module = await _context.CourseModules
          .FirstOrDefaultAsync(c => c.Id == id);

      if (module is null)
      {
        return NotFound();
      }

      return Ok(_mapper.Map<CourseModuleSummaryDto>(module));
    }

    

    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> CreateModule(CreateCourseModuleRequestDto dto)
    {
      if (dto.EndDate <= dto.StartDate)
      {
        return BadRequest("The module seem to end before it starts, check the start and end dates.");
      }

      var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);

      if (course is null)
      {
        return NotFound("Course not found.");
      }

      var isEnrolledAsTeacher = await IsEnrolledAsTeacher(course.Id);

      if (!isEnrolledAsTeacher)
      {
        return Forbid();
      }

      if (dto.StartDate < course.StartDate || dto.EndDate > course.EndDate)
      {
        Console.WriteLine($"module start: {dto.StartDate}, course start: {course.StartDate}");
        Console.WriteLine($"module end: {dto.EndDate}, course end: {course.EndDate}");
        return BadRequest("The module extends past the timeframe of the course, check the start and end dates.");
      }

      var module = new CourseModule
      {
        CourseId = dto.CourseId,
        Name = dto.Name,
        Description = dto.Description,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate
      };

      _context.CourseModules.Add(module);

      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetModule), new { id = module.Id }, _mapper.Map<CourseModuleSummaryDto>(module));
    }

    [HttpPost("/api/courses/{courseid:int}/modules")]
    [Authorize(Roles = "teacher")]
    public async Task<IActionResult> CreateModuleInCourse(int courseid, CreateCourseModuleRequestDto dto)
    {
      if (courseid != dto.CourseId)
      {
        return BadRequest("Course id mismatch between request body and route.");
      }
      return await CreateModule(dto);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "teacher")]
    public async Task<IActionResult> UpdateModule(int id, UpdateCourseModuleRequestDto dto)
    {
      var module = await _context.CourseModules.FirstOrDefaultAsync(m => m.Id == id);
      if (module is null)
      {
        return NotFound();
      }
      var courseId = dto.CourseId ?? module.CourseId;

      var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
      if (course is null)
      {
        return NotFound("Course not found.");
      }

      var isEnrolledAsTeacher = await IsEnrolledAsTeacher(course.Id);

      if (!isEnrolledAsTeacher)
      {
        return Forbid();
      }

      var startDate = dto.StartDate ?? module.StartDate;
      var endDate = dto.EndDate ?? module.EndDate;

      if (endDate <= startDate)
      {
        return BadRequest("The course seem to end before it starts, check the start and end dates.");
      }

      if (dto.StartDate <= course.StartDate || dto.EndDate >= course.EndDate)
      {
        return BadRequest("The module extends past the timeframe of the course, check the start and end dates.");
      }


      if (dto.Name is not null)
      {
        module.Name = dto.Name;
      }

      if (dto.Description is not null)
      {
        module.Description = dto.Description;
      }

      if (module.CourseId != courseId)
      {
        module.CourseId = courseId;
      }

      module.StartDate = startDate;
      module.EndDate = endDate;

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<CourseModuleSummaryDto>(module));
    }


    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher")]
    public async Task<IActionResult> DeleteModule(int id)
    {
      var module = await _context.CourseModules.FirstOrDefaultAsync(c => c.Id == id);
      if (module is null)
      {
        return NotFound();
      }

      var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == module.CourseId);
      if (course is null)
      {
        return NotFound("Course not found.");
      }

      var isEnrolledAsTeacher = await IsEnrolledAsTeacher(course.Id);

      if (!isEnrolledAsTeacher)
      {
        return Forbid();
      }

      _context.CourseModules.Remove(module);
      await _context.SaveChangesAsync();

      return NoContent();
    }
    private async Task<bool> IsEnrolledAsTeacher(int courseId)
    {
      return await _context.CourseEnrollments
          .AnyAsync(e => e.CourseId == courseId
              && e.UserId == CurrentUserId
              && e.Role == CourseRole.Teacher);
    }
  }

}

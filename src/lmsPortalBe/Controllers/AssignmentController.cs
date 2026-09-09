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
  [Route("api/[controller]")]
  public class AssignmentsController(
      ILmsPortalContext context,
      IMapper mapper)
      : CoursePortalControllerBase(context, mapper)
  {

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllAssignments()
    {
      var assignments = await _context.Assignments
          .OrderBy(a => a.DueDate)
          .ToListAsync();

      return Ok(assignments.Select(_mapper.Map<AssignmentDto>));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetUserAssignments()
    {
      var enrolledCourses = await _context.CourseEnrollments
        .Where(e => e.UserId == CurrentUserId)
        .Select(e => e.CourseId)
        .ToListAsync();

      var query = _context.CourseModules
          .Where(m => enrolledCourses.Contains(m.CourseId))
          .SelectMany(m => m.Assignments);

      return Ok(await BuildStudentAssignmentsAsync(query));
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetUserCurrentAssignments()
    {
      var enrolledCourses = await _context.CourseEnrollments
          .Where(e => e.UserId == CurrentUserId)
          .Select(e => e.CourseId)
          .ToListAsync();

      var query = _context.CourseModules
          .Where(m => enrolledCourses.Contains(m.CourseId))
          .SelectMany(m => m.Assignments)
          .Where(a => a.DueDate >= DateTime.UtcNow);

      return Ok(await BuildStudentAssignmentsAsync(query));
    }

    private async Task<List<StudentAssignmentDto>> BuildStudentAssignmentsAsync(IQueryable<Assignment> assignmentsQuery)
    {
      var assignments = await assignmentsQuery
          .OrderBy(a => a.DueDate)
          .ToListAsync();

      var mySubmissions = await _context.Submissions
          .Where(s => s.StudentId == CurrentUserId)
          .ToListAsync();

      var latestByAssignment = mySubmissions
          .Where(s => s.AssignmentId.HasValue)
          .GroupBy(s => s.AssignmentId!.Value)
          .ToDictionary(
              g => g.Key,
              g => g.OrderByDescending(s => s.Id).First());

      return assignments.Select(a =>
      {
        var dto = _mapper.Map<StudentAssignmentDto>(a);

        if (latestByAssignment.TryGetValue(a.Id, out var submission))
        {
          dto.LatestSubmissionId = submission.Id;
          dto.LatestSubmissionStatus = submission.Status.ToString();
          dto.LatestFeedback = submission.Feedback;
        }

        return dto;
      }).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAssignment(int id)
    {
      var assignment = await _context.Assignments
          .Include(a => a.Module)
          .FirstOrDefaultAsync(c => c.Id == id);
      if (assignment is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsEnrolledAsync(assignment.Module.CourseId))
      {
        return Forbid();
      }

      return Ok(_mapper.Map<AssignmentDto>(assignment));
    }

    [HttpGet("/api/modules/{id:int}/assignments")]
    public async Task<IActionResult> GetModuleAssignments(int id)
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

      var assignments = await _context.Assignments
          .Where(a => a.ModuleId == id)
          .OrderBy(a => a.DueDate)
          .ToListAsync();

      return Ok(assignments.Select(_mapper.Map<AssignmentDto>));
    }

    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> CreateAssignment(CreateAssignmentRequestDto dto)
    {

      var module = await _context.CourseModules.FirstOrDefaultAsync(c => c.Id == dto.ModuleId);
      if (module is null)
      {
        return NotFound("Cannot find module to add assignment to.");
      }

      if (!User.IsInRole("admin") && !await IsCourseTeacherAsync(module.CourseId))
      {
        return Forbid();
      }

      if (dto.DueDate < module.StartDate || dto.DueDate > module.EndDate)
      {
        return BadRequest("The due date is outside the time frame of the module.");
      }

      var assignment = new Assignment
      {
        ModuleId = dto.ModuleId,
        Name = dto.Name,
        Description = dto.Description,
        DueDate = dto.DueDate,
      };

      _context.Assignments.Add(assignment);

      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetAssignment), new { id = assignment.Id }, _mapper.Map<AssignmentDto>(assignment));
    }


    [HttpPost("/api/modules/{moduleId:int}/assignments")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> CreateAssignmentInModule(int moduleId, CreateAssignmentRequestDto dto)
    {
      if (dto.ModuleId != moduleId)
      {
        return BadRequest("Module Id in request body does not match id in route.");
      }
      return await CreateAssignment(dto);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> UpdateAssignment(int id, UpdateAssignmentRequestDto dto)
    {
      var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == id);
      if (assignment is null)
      {
        return NotFound();
      }

      var dueDate = dto.DueDate ?? assignment.DueDate;

      var module = await _context.CourseModules.FirstOrDefaultAsync(c => c.Id == assignment.ModuleId);
      if (module is null)
      {
        return NotFound("Cannot find parent module.");
      }

      if (!User.IsInRole("admin") && !await IsCourseTeacherAsync(module.CourseId))
      {
        return Forbid();
      }

      if (dto.ModuleId is not null && dto.ModuleId != assignment.ModuleId)
      {
        var destinationModule = await _context.CourseModules.FirstOrDefaultAsync(c => c.Id == dto.ModuleId.Value);
        if (destinationModule is null)
        {
          return NotFound("Cannot find destination module.");
        }

        if (!User.IsInRole("admin") && !await IsCourseTeacherAsync(destinationModule.CourseId))
        {
          return Forbid();
        }

        assignment.ModuleId = destinationModule.Id;
        module = destinationModule;
      }

      if (dueDate < module.StartDate || dueDate > module.EndDate)
      {
        return BadRequest("Due date must be within module's timeframe.");
      }

      if (dto.Name is not null)
      {
        assignment.Name = dto.Name;
      }

      if (dto.Description is not null)
      {
        assignment.Description = dto.Description;
      }

      assignment.DueDate = dueDate;

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<AssignmentDto>(assignment));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
      var assignment = await _context.Assignments
          .Include(c => c.Module)
          .Include(c => c.Submissions)
          .FirstOrDefaultAsync(c => c.Id == id);
      if (assignment is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsCourseTeacherAsync(assignment.Module.CourseId))
      {
        return Forbid();
      }

      _context.Assignments.Remove(assignment);
      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}

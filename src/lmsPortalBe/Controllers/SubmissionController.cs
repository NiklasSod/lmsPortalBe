using System.Security.Claims;
using AutoMapper;
using lmsPortalBe.Data;
using lmsPortalBe.DTOs.Course;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Controllers
{
  [Route("api/[controller]")]
  public class SubmissionsController(
      ILmsPortalContext context,
      IMapper mapper,
      UserManager<ApplicationUser> userManager) 
      : CoursePortalControllerBase(context, mapper)
  {
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllSubmissions()
    {
      var submission = await _context.Submissions
          .OrderBy(a => a.HandinDate)
          .ToListAsync();

      return Ok(submission.Select(_mapper.Map<SubmissionDto>));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetUserSubmissions()
    {
      var user = await _userManager.FindByIdAsync(CurrentUserId);
      if (user is null)
      {
        return NotFound();
      }

      return Ok(user.Submissions.Select(_mapper.Map<SubmissionDto>));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSubmission(int id)
    {
      var submission = await _context.Submissions
          .Include(s => s.Assignment)
          .ThenInclude(a => a.Module)
          .FirstOrDefaultAsync(s => s.Id == id);

      if (submission is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") 
          && !await IsCourseTeacherAsync(submission.Assignment.Module.CourseId)
          && submission.StudentId != CurrentUserId)
      {
        return Forbid();
      }

      return Ok(_mapper.Map<SubmissionDto>(submission));
    }

    [HttpGet("/api/assignments/{id:int}/submissions")]
    public async Task<IActionResult> GetAssignmentSubmissions(int id)
    {
      var assignment = await _context.Assignments
          .Include(a => a.Module)
          .FirstOrDefaultAsync(c => c.Id == id);
      if (assignment is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") 
          && !await IsCourseTeacherAsync(assignment.Module.CourseId))
      {
        return Forbid();
      }

      var submissions = await _context.Submissions
          .Where(s => s.AssignmentId == id)
          .OrderBy(s => s.HandinDate)
          .ToListAsync();

      return Ok(submissions.Select(_mapper.Map<SubmissionDto>));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubmission(CreateSubmissionRequestDto dto)
    {

      var assignment = await _context.Assignments
        .Include(a => a.Module)
        .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId);

      if (assignment is null)
      {
        return NotFound("Cannot find module to add assignment to.");
      }

      if (!User.IsInRole("admin") 
          && !await IsEnrolledAsync(assignment.Module.CourseId))
      {
        return Forbid();
      }

      if (dto.Feedback is not null 
          && !User.IsInRole("admin")
          && !await IsCourseTeacherAsync(assignment.Module.CourseId))
      {
        return Forbid("No permission to include feedback");
      }

      var submission = new Submission
      {
        AssignmentId = dto.AssignmentId,
        StudentId = dto.StudentId,
        Content = dto.Content,
        Feedback = dto.Feedback ?? string.Empty,
        HandinDate = dto.HandinDate,
      };

      _context.Submissions.Add(submission);

      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetSubmission), new { id = submission.Id }, _mapper.Map<SubmissionDto>(submission));
    }


    [HttpPost("/api/assignments/{id:int}")]
    public async Task<IActionResult> CreateSubmissionInAssignment(int id, CreateSubmissionRequestDto dto)
    {
      if (dto.AssignmentId != id)
      {
        return BadRequest("Assignment Id in request body does not match id in route.");
      }
      return await CreateSubmission(dto);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> UpdateSubmission(int id, UpdateSubmissionRequestDto dto)
    {
      var submission = await _context.Submissions
        .Include(s => s.Assignment)
        .ThenInclude(a => a.Module)
        .FirstOrDefaultAsync(s => s.Id == id);
      if (submission is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") 
            && !await IsCourseTeacherAsync(submission.Assignment.Module.CourseId) 
            && submission.StudentId != CurrentUserId)
      {
        return Forbid();
      }
      
      if (dto.AssignmentId is not null)
      {
        if (!User.IsInRole("admin"))
        {
            return Forbid("No permission to change assignment.");
        }

        var assignment = await _context.Assignments
          .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId);

        if (assignment is null)
        {
            return NotFound("Destination assignment does not exist.");
        }

        submission.AssignmentId = (int)dto.AssignmentId;
      }

      if (dto.StudentId is not null) {
        
        if (!User.IsInRole("admin"))
        {
            return Forbid("No permission to change student.");
        }

        var student = await _userManager.FindByIdAsync(dto.StudentId);

        if (student is null)
        {
            return NotFound("Destination student does not exist.");
        }

        submission.StudentId = dto.StudentId;
      }

      if (dto.Feedback is not null) 
      {
        
        if (!User.IsInRole("admin") 
            && !await IsCourseTeacherAsync(submission.Assignment.Module.CourseId))
        {
            return Forbid("No permission to send feedback.");
        }

        if (dto.Status is null 
          && submission.Status != AssignmentStatus.Revision
          && submission.Status != AssignmentStatus.Approved)
        {
          return BadRequest("Must update status when supplying feedback.");
        }

        submission.Feedback = dto.Feedback;
      }

      if (dto.HandinDate is not null)
      {
        if (submission.HandinDate is not null
              && !User.IsInRole("admin"))
        {
          return Forbid("No permission to change handin date after the fact.");
        }
        submission.HandinDate = dto.HandinDate;
      }

      submission.Content = dto.Content ?? submission.Content;

      if (dto.Status is not null)
      {
        if(!Enum.TryParse<AssignmentStatus>(dto.Status, ignoreCase: true, out var status))
        {
          return BadRequest("Cannot recognize submission status.");
        }

        switch (status)
        {
          case AssignmentStatus.Approved | AssignmentStatus.Revision:
            if (!User.IsInRole("admin") 
                && !await IsCourseTeacherAsync(submission.Assignment.Module.CourseId))
            {
              return Forbid("No permission to send feedback.");
            }

            if (dto.Feedback is null && submission.Feedback is null)
            {
              return BadRequest("Must supply feedback when returning assignment.");
            }
          break;
          case AssignmentStatus.HandedIn | AssignmentStatus.Unsent:
            if (!User.IsInRole("admin")  
                && submission.StudentId != CurrentUserId)
            {
              return Forbid();
            }
            break;
        }

        submission.Status = status;
      }

      

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<SubmissionDto>(submission));
    }

    [HttpDelete("{id:int}")]
    [Authorize("admin")]
    public async Task<IActionResult> DeleteSubmission(int id)
    {
      var submission = await _context.Submissions
          .Include(s => s.Assignment)
          .ThenInclude(a => a.Module)
          .FirstOrDefaultAsync(s => s.Id == id);

      if (submission is null)
      {
        return NotFound();
      }

      _context.Submissions.Remove(submission);
      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}

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
  public class SubmissionsController(
      ILmsPortalContext context,
      IMapper mapper)
      : CoursePortalControllerBase(context, mapper)
  {

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllSubmissions()
    {
      var submissions = await _context.Submissions
          .OrderBy(s => s.HandinDate)
          .ToListAsync();

      return Ok(submissions.Select(_mapper.Map<SubmissionDto>));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMySubmissions()
    {
      var submissions = await _context.Submissions
          .Where(s => s.StudentId == CurrentUserId)
          .OrderByDescending(s => s.HandinDate)
          .ToListAsync();

      return Ok(submissions.Select(_mapper.Map<SubmissionDto>));
    }

    [HttpGet("pending")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> GetPendingSubmissions()
    {
      IQueryable<Submission> query = _context.Submissions
          .Where(s => s.Status == AssignmentStatus.HandedIn);

      if (!User.IsInRole("admin"))
      {
        // A teacher only sees pending submissions for courses they teach.
        query = query.Where(s => s.Assignment != null
            && _context.CourseEnrollments.Any(e =>
                e.UserId == CurrentUserId
                && e.Role == CourseRole.Teacher
                && e.CourseId == s.Assignment!.Module.CourseId));
      }

      var submissions = await query
          .OrderByDescending(s => s.HandinDate)
          .ThenByDescending(s => s.Id)
          .ToListAsync();

      return Ok(submissions.Select(_mapper.Map<SubmissionDto>));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSubmission(int id)
    {
      var submission = await FindSubmissionAsync(id);
      if (submission is null)
      {
        return NotFound();
      }

      if (!await CanAccessAsync(submission))
      {
        return Forbid();
      }

      return Ok(_mapper.Map<SubmissionDto>(submission));
    }

    [HttpGet("/api/assignments/{assignmentId:int}/submissions")]
    public async Task<IActionResult> GetAssignmentSubmissions(int assignmentId)
    {
      var assignment = await _context.Assignments
          .Include(a => a.Module)
          .FirstOrDefaultAsync(a => a.Id == assignmentId);
      if (assignment is null)
      {
        return NotFound();
      }

      if (!User.IsInRole("admin") && !await IsCourseTeacherAsync(assignment.Module.CourseId))
      {
        return Forbid();
      }

      var submissions = await _context.Submissions
          .Where(s => s.AssignmentId == assignmentId)
          .OrderByDescending(s => s.HandinDate)
          .ToListAsync();

      return Ok(submissions.Select(_mapper.Map<SubmissionDto>));
    }

    [HttpPost]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> HandInSubmission(CreateSubmissionRequestDto dto)
    {
      var assignment = await _context.Assignments
          .Include(a => a.Module)
          .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId);
      if (assignment is null)
      {
        return NotFound("Assignment not found.");
      }

      if (!await IsEnrolledAsStudentAsync(assignment.Module.CourseId))
      {
        return Forbid();
      }

      var latest = await _context.Submissions
          .Where(s => s.AssignmentId == assignment.Id && s.StudentId == CurrentUserId)
          .OrderByDescending(s => s.Id)
          .FirstOrDefaultAsync();

      if (latest is not null)
      {
        if (latest.Status == AssignmentStatus.Approved)
        {
          return Conflict("This assignment has already been approved.");
        }

        if (latest.Status == AssignmentStatus.HandedIn)
        {
          return Conflict("You have already handed in this assignment; wait for the teacher's feedback.");
        }
      }

      var submission = new Submission
      {
        AssignmentId = assignment.Id,
        StudentId = CurrentUserId,
        Content = dto.Content,
        Status = AssignmentStatus.HandedIn,
        HandinDate = DateTime.UtcNow
      };

      _context.Submissions.Add(submission);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetSubmission), new { id = submission.Id }, _mapper.Map<SubmissionDto>(submission));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> GradeSubmission(int id, UpdateSubmissionRequestDto dto)
    {
      var submission = await FindSubmissionAsync(id);
      if (submission is null)
      {
        return NotFound();
      }

      if (!await CanGradeAsync(submission))
      {
        return Forbid();
      }

      var assignmentId = submission.AssignmentId;
      if (assignmentId is not null)
      {
        var latestId = await _context.Submissions
            .Where(s => s.AssignmentId == assignmentId
                && s.StudentId == submission.StudentId)
            .OrderByDescending(s => s.Id)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (submission.Id != latestId)
        {
          return BadRequest("Only the latest submission can be graded.");
        }
      }

      if (dto.Feedback is null && dto.Status is null)
      {
        return BadRequest("Provide feedback and/or a status to grade the submission.");
      }

      if (dto.Status is not null)
      {
        if (!Enum.TryParse<AssignmentStatus>(dto.Status, ignoreCase: true, out var status)
            || status is not (AssignmentStatus.Approved or AssignmentStatus.Revision))
        {
          return BadRequest("Status must be either 'Approved' or 'Revision'.");
        }

        submission.Status = status;
      }

      if (dto.Feedback is not null)
      {
        submission.Feedback = dto.Feedback;
      }

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<SubmissionDto>(submission));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSubmission(int id)
    {
      var submission = await FindSubmissionAsync(id);
      if (submission is null)
      {
        return NotFound();
      }

      if (!await CanDeleteAsync(submission))
      {
        return Forbid();
      }

      _context.Submissions.Remove(submission);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private async Task<bool> CanGradeAsync(Submission submission)
    {
      if (User.IsInRole("admin"))
      {
        return true;
      }

      var assignment = submission.Assignment;
      if (assignment is null)
      {
        return false;
      }

      return await IsCourseTeacherAsync(assignment.Module.CourseId);
    }

    private async Task<bool> CanDeleteAsync(Submission submission)
    {
      if (User.IsInRole("admin") || submission.StudentId == CurrentUserId)
      {
        return true;
      }

      var assignment = submission.Assignment;
      if (assignment is null)
      {
        return false;
      }

      return await IsCourseTeacherAsync(assignment.Module.CourseId);
    }

    private async Task<Submission?> FindSubmissionAsync(int id) =>
        await _context.Submissions
            .Include(s => s.Assignment)
            .ThenInclude(a => a!.Module)
            .FirstOrDefaultAsync(s => s.Id == id);

    private async Task<bool> CanAccessAsync(Submission submission)
    {
      if (User.IsInRole("admin"))
      {
        return true;
      }

      if (submission.StudentId == CurrentUserId)
      {
        return true;
      }

      // An orphaned submission (its assignment was deleted) has no assignment
      // left to derive a course from, so only the owner or an admin may access it.
      var assignment = submission.Assignment;
      if (assignment is null)
      {
        return false;
      }

      return await IsCourseTeacherAsync(assignment.Module.CourseId);
    }
  }
}

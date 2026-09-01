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
  public class CoursesController(
      ILmsPortalContext context,
      IMapper mapper) : ControllerBase
  {
    private readonly ILmsPortalContext _context = context;
    private readonly IMapper _mapper = mapper;

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");

    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
      var courses = await _context.Courses
          .OrderBy(c => c.StartDate)
          .ToListAsync();

      return Ok(courses.Select(_mapper.Map<CourseSummaryDto>));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourse(int id)
    {
      var course = await _context.Courses
          .Include(c => c.Enrollments)
              .ThenInclude(e => e.User)
          .FirstOrDefaultAsync(c => c.Id == id);

      if (course is null)
      {
        return NotFound();
      }

      return Ok(_mapper.Map<CourseDetailDto>(course));
    }

    [HttpPost]
    [Authorize(Roles = "teacher")]
    public async Task<IActionResult> CreateCourse(CreateCourseRequestDto dto)
    {
      if (dto.EndDate <= dto.StartDate)
      {
        return BadRequest("The course seem to end before it starts, check the start and end dates.");
      }

      var course = new CourseModel
      {
        Name = dto.Name,
        Description = dto.Description,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate
      };

      _context.Courses.Add(course);
      _context.CourseEnrollments.Add(new CourseEnrollment
      {
        Course = course,
        UserId = CurrentUserId,
        Role = CourseRole.Teacher
      });

      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, _mapper.Map<CourseSummaryDto>(course));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "teacher")]
    public async Task<IActionResult> UpdateCourse(int id, UpdateCourseRequestDto dto)
    {
      var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
      if (course is null)
      {
        return NotFound();
      }

      var startDate = dto.StartDate ?? course.StartDate;
      var endDate = dto.EndDate ?? course.EndDate;

      if (endDate <= startDate)
      {
        return BadRequest("The course seem to end before it starts, check the start and end dates.");
      }

      if (dto.Name is not null)
      {
        course.Name = dto.Name;
      }

      if (dto.Description is not null)
      {
        course.Description = dto.Description;
      }

      course.StartDate = startDate;
      course.EndDate = endDate;

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<CourseSummaryDto>(course));
    }

    [HttpPost("enroll")]
    public async Task<IActionResult> Enroll(EnrollRequestDto dto)
    {
      var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
      if (course is null)
      {
        return NotFound("Course not found.");
      }

      var userId = CurrentUserId;
      var role = User.IsInRole("teacher") ? CourseRole.Teacher : CourseRole.Student;

      if (await _context.CourseEnrollments
              .AnyAsync(e => e.UserId == userId && e.CourseId == course.Id))
      {
        return BadRequest("Already enrolled in this course.");
      }

      if (role == CourseRole.Student)
      {
        var hasOverlap = await _context.CourseEnrollments
            .Include(e => e.Course)
            .AnyAsync(e => e.UserId == userId
                && e.Course.StartDate <= course.EndDate
                && course.StartDate <= e.Course.EndDate);

        if (hasOverlap)
        {
          return BadRequest("Enrollment conflicts with another course that overlaps this course's schedule.");
        }
      }

      _context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = course.Id,
        UserId = userId,
        Role = role
      });

      await _context.SaveChangesAsync();

      return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
      var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
      if (course is null)
      {
        return NotFound();
      }

      var isCreator = await _context.CourseEnrollments
          .AnyAsync(e => e.CourseId == id
              && e.UserId == CurrentUserId
              && e.Role == CourseRole.Teacher);

      if (!isCreator)
      {
        return Forbid();
      }

      _context.Courses.Remove(course);
      await _context.SaveChangesAsync();

      return NoContent();
    }
  }
}

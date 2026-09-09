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
    [Authorize]
    public class CoursePortalControllerBase(
        ILmsPortalContext context,
        IMapper mapper) : ControllerBase
    {
        protected readonly ILmsPortalContext _context = context;
        protected readonly IMapper _mapper = mapper;

        protected string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found.");

        protected async Task<bool> IsCourseTeacherAsync(int courseId) =>
            await _context.CourseEnrollments.AnyAsync(e => e.CourseId == courseId
                && e.UserId == CurrentUserId
                && e.Role == CourseRole.Teacher);

        protected async Task<bool> IsEnrolledAsync(int courseId) =>
            await _context.CourseEnrollments.AnyAsync(e => e.CourseId == courseId
                && e.UserId == CurrentUserId);

        protected async Task<bool> IsEnrolledAsStudentAsync(int courseId) =>
            await _context.CourseEnrollments.AnyAsync(e => e.CourseId == courseId
                && e.UserId == CurrentUserId
                && e.Role == CourseRole.Student);
    }
}

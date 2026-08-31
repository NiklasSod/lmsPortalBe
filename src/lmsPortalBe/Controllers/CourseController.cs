using System.Security.Claims;
using AutoMapper;
using lmsPortalBe.Data;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  [Authorize(Roles = "student,teacher")]
  public class CoursesController(
    UserManager<ApplicationUser> userManager,
    ILmsPortalContext context,
    IMapper mapper) : ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILmsPortalContext _context = context;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
      var courses = await _context.Courses.ToListAsync();
      if (courses is null)
      {
        return NotFound();
      }

      return Ok(courses.Select(_mapper.Map<CourseSummaryDto>));
    }
  }
}

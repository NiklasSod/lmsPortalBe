using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace lmsPortalBe.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  [Authorize(Roles = "admin")]
  public class AdminController(UserManager<ApplicationUser> userManager) : ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole(AssignRoleRequestDto dto)
    {
      var user = await _userManager.FindByEmailAsync(dto.Email);
      if (user is null)
      {
        return NotFound("User not found.");
      }

      // Keep student/teacher mutually exclusive. The admin role is never
      // touched here to avoid privilege escalation.
      foreach (var role in new[] { "student", "teacher" })
      {
        if (await _userManager.IsInRoleAsync(user, role))
        {
          await _userManager.RemoveFromRoleAsync(user, role);
        }
      }

      var result = await _userManager.AddToRoleAsync(user, dto.Role);
      if (!result.Succeeded)
      {
        return BadRequest(result.Errors.Select(e => e.Description));
      }

      return NoContent();
    }
  }
}

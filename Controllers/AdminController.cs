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
  [Authorize(Roles = "admin")]
  public class AdminController(
      UserManager<ApplicationUser> userManager,
      ILmsPortalContext context) : ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILmsPortalContext _context = context;

    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole(AssignRoleRequestDto dto)
    {
      var user = await _userManager.FindByEmailAsync(dto.Email);
      if (user is null)
      {
        return NotFound("User not found.");
      }

      var targetRole = dto.Role.Trim().ToLowerInvariant();
      var conflictingRole = targetRole == "student" ? "teacher" : "student";

      var currentRoles = await _userManager.GetRolesAsync(user);

      if (currentRoles.Contains("admin", StringComparer.OrdinalIgnoreCase))
      {
        return BadRequest("Cannot assign a student or teacher role to an administrator.");
      }

      await using var transaction = await _context.Database.BeginTransactionAsync();

      if (currentRoles.Contains(conflictingRole, StringComparer.OrdinalIgnoreCase))
      {
        var removeResult = await _userManager.RemoveFromRoleAsync(user, conflictingRole);
        if (!removeResult.Succeeded)
        {
          return BadRequest(removeResult.Errors.Select(e => e.Description));
        }
      }

      if (!currentRoles.Contains(targetRole, StringComparer.OrdinalIgnoreCase))
      {
        var addResult = await _userManager.AddToRoleAsync(user, targetRole);
        if (!addResult.Succeeded)
        {
          return BadRequest(addResult.Errors.Select(e => e.Description));
        }
      }

      await transaction.CommitAsync();

      return NoContent();
    }
  }
}

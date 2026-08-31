using System.Security.Claims;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace lmsPortalBe.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  [Authorize(Roles = "student,teacher")]
  public class AccountController(UserManager<ApplicationUser> userManager) : ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpDelete]
    public async Task<IActionResult> DeleteSelf()
    {
      var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userId))
      {
        return Unauthorized("User identity not found.");
      }

      var user = await _userManager.FindByIdAsync(userId);
      if (user is null)
      {
        return NotFound("User not found.");
      }

      var result = await _userManager.DeleteAsync(user);
      if (!result.Succeeded)
      {
        return BadRequest(result.Errors.Select(e => e.Description));
      }

      return NoContent();
    }
  }
}

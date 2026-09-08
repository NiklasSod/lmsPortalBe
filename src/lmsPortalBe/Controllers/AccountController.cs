using System.Security.Claims;
using lmsPortalBe.DTOs.User;
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

    [HttpPut]
    public async Task<IActionResult> UpdateSelf(UpdateUserRequestDto dto)
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

      user.FirstName = dto.FirstName;
      user.LastName = dto.LastName;

      if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
      {
        var emailResult = await _userManager.SetEmailAsync(user, dto.Email);
        if (!emailResult.Succeeded)
        {
          return BadRequest(emailResult.Errors.Select(e => e.Description));
        }

        var userNameResult = await _userManager.SetUserNameAsync(user, dto.Email);
        if (!userNameResult.Succeeded)
        {
          return BadRequest(userNameResult.Errors.Select(e => e.Description));
        }
      }
      else
      {
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
          return BadRequest(updateResult.Errors.Select(e => e.Description));
        }
      }

      return NoContent();
    }

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

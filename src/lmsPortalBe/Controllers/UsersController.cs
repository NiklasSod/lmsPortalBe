using lmsPortalBe.DTOs.User;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace lmsPortalBe.Controllers
{
  [ApiController]
  [Route("api/users")]
  [Authorize(Roles = "admin,teacher")]
  public class UsersController(UserManager<ApplicationUser> userManager) : ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserRequestDto dto)
    {
      var user = await _userManager.FindByIdAsync(id);
      if (user is null)
      {
        return NotFound("User not found.");
      }

      var roles = await _userManager.GetRolesAsync(user);
      if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase))
      {
        return BadRequest("Cannot edit an administrator.");
      }

      var isStudentOrTeacher = roles.Contains("student", StringComparer.OrdinalIgnoreCase)
          || roles.Contains("teacher", StringComparer.OrdinalIgnoreCase);
      if (!isStudentOrTeacher)
      {
        return BadRequest("Only users with a student or teacher role can be edited.");
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
  }
}

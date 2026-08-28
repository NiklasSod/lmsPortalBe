using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Auth;

public class AssignRoleRequestDto
{
  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;

  [Required]
  [RegularExpression("^(student|teacher)$", ErrorMessage = "Role must be either 'student' or 'teacher'.")]
  public string Role { get; set; } = string.Empty;
}

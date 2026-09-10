using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Auth;

public class ChangePasswordRequestDto
{
  [Required]
  public string CurrentPassword { get; set; } = string.Empty;

  [Required]
  [MinLength(8)]
  [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
      ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, and one digit.")]
  public string NewPassword { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.User;

public class UpdateUserRequestDto
{
  [Required]
  [MinLength(2)]
  public string FirstName { get; set; } = string.Empty;

  [Required]
  [MinLength(2)]
  public string LastName { get; set; } = string.Empty;

  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;
}

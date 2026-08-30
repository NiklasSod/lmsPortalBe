using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Auth;

public class RegisterRequestDto
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

    [Required]
    [MinLength(8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, and one digit.")]
    public string Password { get; set; } = string.Empty;
}

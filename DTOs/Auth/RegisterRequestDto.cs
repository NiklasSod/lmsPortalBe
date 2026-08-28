using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, and one digit.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(student|teacher)$", ErrorMessage = "Role must be either 'student' or 'teacher'.")]
    public string Role { get; set; } = string.Empty;
}

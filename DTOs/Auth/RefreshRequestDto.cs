using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Auth;

public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

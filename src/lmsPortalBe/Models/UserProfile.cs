using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.Models
{
  public class UserProfile
  {
    [Key]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public string? AboutMe { get; set; }

    public string? GitHubLink { get; set; }

    public List<string> Skills { get; set; } = [];

    public string? WhatsAppNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }
  }
}

namespace lmsPortalBe.DTOs.UserProfile;

public class CreateUserProfileRequestDto
{
  public string? AboutMe { get; set; }
  public string? GitHubLink { get; set; }
  public List<string> Skills { get; set; } = [];
  public string? WhatsAppNumber { get; set; }
  public DateOnly? DateOfBirth { get; set; }
}

namespace lmsPortalBe.Services;

/// <summary>
/// A freshly issued access + refresh token pair. The refresh token is handed to
/// the HTTP layer to be stored in an HttpOnly cookie, so it never leaves the
/// server as part of a JSON response body.
/// </summary>
public class AuthTokens
{
  public string AccessToken { get; set; } = string.Empty;
  public string RefreshToken { get; set; } = string.Empty;
  public DateTime ExpiresAt { get; set; }
  public DateTime RefreshTokenExpiresAt { get; set; }
}

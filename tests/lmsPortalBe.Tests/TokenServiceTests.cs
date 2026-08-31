using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using lmsPortalBe.Models;
using lmsPortalBe.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace lmsPortalBe.Tests;

public class TokenServiceTests : IClassFixture<TestWebApplicationFactory>
{
  private readonly TestWebApplicationFactory _factory;

  public TokenServiceTests(TestWebApplicationFactory factory)
  {
    _factory = factory;
  }

  private static ApplicationUser NewUser(string email) => new()
  {
    UserName = email,
    Email = email,
    EmailConfirmed = true
  };

  [Fact]
  public async Task CreateAccessToken_ContainsSubjectEmailAndRoleClaims()
  {
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

    var user = NewUser("claims@example.com");
    await userManager.CreateAsync(user, "Passw0rd1");

    var token = tokenService.CreateAccessToken(user, new List<string> { "student" });

    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
    Assert.Equal(user.Id, jwt.Subject);
    Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "claims@example.com");
    Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "student");
  }

  [Fact]
  public async Task CreateTokensAsync_ReturnsTokenPair()
  {
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

    var user = NewUser("create.tokens@example.com");
    await userManager.CreateAsync(user, "Passw0rd1");

    var result = await tokenService.CreateTokensAsync(user, new List<string> { "student" });

    Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
    Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    Assert.NotEqual(default, result.ExpiresAt);
  }

  [Fact]
  public async Task RefreshTokenAsync_RotatesRefreshToken()
  {
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

    var user = NewUser("rotate@example.com");
    await userManager.CreateAsync(user, "Passw0rd1");

    var first = await tokenService.CreateTokensAsync(user, new List<string> { "student" });
    var second = await tokenService.RefreshTokenAsync(first.RefreshToken);

    Assert.NotEqual(first.RefreshToken, second.RefreshToken);
    Assert.False(string.IsNullOrWhiteSpace(second.AccessToken));
  }

  [Fact]
  public async Task RevokeRefreshTokenAsync_PreventsFurtherRefresh()
  {
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

    var user = NewUser("revoke@example.com");
    await userManager.CreateAsync(user, "Passw0rd1");

    var pair = await tokenService.CreateTokensAsync(user, new List<string> { "student" });
    await tokenService.RevokeRefreshTokenAsync(pair.RefreshToken);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => tokenService.RefreshTokenAsync(pair.RefreshToken));
  }

  [Fact]
  public async Task RefreshTokenAsync_ReusingRevokedToken_RevokesAllTokensForUser()
  {
    string firstToken;
    string secondToken;

    // Each scope models a separate HTTP request (a fresh DbContext), which is
    // how the application actually consumes the token service.
    using (var scope = _factory.Services.CreateScope())
    {
      var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
      var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

      var user = NewUser("reuse@example.com");
      await userManager.CreateAsync(user, "Passw0rd1");

      var first = await tokenService.CreateTokensAsync(user, new List<string> { "student" });
      var second = await tokenService.RefreshTokenAsync(first.RefreshToken);
      firstToken = first.RefreshToken;
      secondToken = second.RefreshToken;
    }

    // Presenting an already-rotated token is treated as token theft.
    using (var scope = _factory.Services.CreateScope())
    {
      var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

      await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => tokenService.RefreshTokenAsync(firstToken));
    }

    // Every token issued to the user should now be revoked.
    using (var scope = _factory.Services.CreateScope())
    {
      var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

      await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => tokenService.RefreshTokenAsync(secondToken));
    }
  }
}

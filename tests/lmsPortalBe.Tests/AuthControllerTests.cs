using System.Net;
using System.Net.Http.Json;
using lmsPortalBe.DTOs.Auth;

namespace lmsPortalBe.Tests;

public class AuthControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public AuthControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  [Fact]
  public async Task Register_WithValidRequest_ReturnsTokenPair()
  {
    var response = await Client.PostAsJsonAsync("/api/auth/register", RegisterRequest("register.ok@example.com"), TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
    Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
    Assert.NotEqual(default, body.ExpiresAt);
  }

  [Fact]
  public async Task Register_WithWeakPassword_ReturnsBadRequest()
  {
    var response = await Client.PostAsJsonAsync("/api/auth/register", RegisterRequest("register.weak@example.com", "weak"), TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
  {
    var request = RegisterRequest("register.dupe@example.com");

    var first = await Client.PostAsJsonAsync("/api/auth/register", request, TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.OK, first.StatusCode);

    var second = await Client.PostAsJsonAsync("/api/auth/register", request, TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
  }

  [Fact]
  public async Task Login_WithValidCredentials_ReturnsTokenPair()
  {
    await RegisterAsync("login.ok@example.com");

    var response = await Client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequestDto { Email = "login.ok@example.com", Password = "Passw0rd1" },
        TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
  }

  [Fact]
  public async Task Login_WithWrongPassword_ReturnsUnauthorized()
  {
    await RegisterAsync("login.wrong@example.com");

    var response = await Client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequestDto { Email = "login.wrong@example.com", Password = "WrongPass1" },
        TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Refresh_WithValidToken_ReturnsNewTokenPair()
  {
    var tokens = await RegisterAsync("refresh.ok@example.com");

    var response = await Client.PostAsJsonAsync(
        "/api/auth/refresh",
        new RefreshRequestDto { RefreshToken = tokens.RefreshToken },
        TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.NotEqual(tokens.RefreshToken, body.RefreshToken);
  }

  [Fact]
  public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
  {
    var response = await Client.PostAsJsonAsync(
        "/api/auth/refresh",
        new RefreshRequestDto { RefreshToken = "not-a-real-token" },
        TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Logout_RevokesRefreshToken()
  {
    var tokens = await RegisterAsync("logout.ok@example.com");

    var logout = await Client.PostAsJsonAsync(
        "/api/auth/logout",
        new RefreshRequestDto { RefreshToken = tokens.RefreshToken },
        TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

    var refresh = await Client.PostAsJsonAsync(
        "/api/auth/refresh",
        new RefreshRequestDto { RefreshToken = tokens.RefreshToken },
        TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
  }
}

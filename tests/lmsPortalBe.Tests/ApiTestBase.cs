using System.Net.Http.Headers;
using System.Net.Http.Json;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.Services;

namespace lmsPortalBe.Tests;

/// <summary>
/// Shared helpers for the integration tests that talk to the HTTP API.
/// </summary>
public abstract class ApiTestBase
{
  protected readonly TestWebApplicationFactory Factory;
  protected readonly HttpClient Client;

  protected ApiTestBase(TestWebApplicationFactory factory)
  {
    Factory = factory;
    Client = factory.CreateClient();
  }

  protected static RegisterRequestDto RegisterRequest(string email, string password = "Passw0rd1") => new()
  {
    FirstName = "Jane",
    LastName = "Doe",
    Email = email,
    Password = password
  };

  protected async Task<AuthResponseDto> RegisterAsync(string email, string password = "Passw0rd1")
  {
    var response = await Client.PostAsJsonAsync(
        "/api/auth/register",
        RegisterRequest(email, password),
        TestContext.Current.CancellationToken);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestContext.Current.CancellationToken))!;
  }

  protected async Task<AuthResponseDto> LoginAsync(string email, string password)
  {
    var response = await Client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequestDto { Email = email, Password = password },
        TestContext.Current.CancellationToken);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestContext.Current.CancellationToken))!;
  }

  protected sealed record AuthResult(AuthResponseDto Body, string RefreshToken);

  protected async Task<AuthResult> RegisterWithTokensAsync(string email, string password = "Passw0rd1")
  {
    var response = await Client.PostAsJsonAsync(
        "/api/auth/register",
        RegisterRequest(email, password),
        TestContext.Current.CancellationToken);
    response.EnsureSuccessStatusCode();

    var body = (await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestContext.Current.CancellationToken))!;
    return new AuthResult(body, ReadRefreshTokenCookie(response));
  }

  protected static string ReadRefreshTokenCookie(HttpResponseMessage response)
  {
    if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
    {
      foreach (var setCookie in setCookies)
      {
        var name = JwtConstants.RefreshTokenCookie + "=";
        var index = setCookie.IndexOf(name, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
          var valueStart = index + name.Length;
          var valueEnd = setCookie.IndexOf(';', valueStart);
          return valueEnd >= 0
              ? setCookie[valueStart..valueEnd]
              : setCookie[valueStart..];
        }
      }
    }

    return string.Empty;
  }

  protected async Task<HttpResponseMessage> SendAuthorizedAsync(
      HttpMethod method,
      string url,
      string accessToken)
  {
    using var request = new HttpRequestMessage(method, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    return await Client.SendAsync(request, TestContext.Current.CancellationToken);
  }

  protected async Task<HttpResponseMessage> SendAuthorizedAsync<T>(
      HttpMethod method,
      string url,
      string accessToken,
      T body)
  {
    using var request = new HttpRequestMessage(method, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    request.Content = JsonContent.Create(body);
    return await Client.SendAsync(request, TestContext.Current.CancellationToken);
  }
}

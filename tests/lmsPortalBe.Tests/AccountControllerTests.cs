using System.Net;
using System.Net.Http.Json;
using lmsPortalBe.DTOs.Auth;

namespace lmsPortalBe.Tests;

public class AccountControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public AccountControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  [Fact]
  public async Task DeleteSelf_WithoutToken_ReturnsUnauthorized()
  {
    var response = await Client.DeleteAsync("/api/account", TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task DeleteSelf_WithStudentToken_DeletesUser()
  {
    var tokens = await RegisterAsync("delete.self@example.com");

    var response = await SendAuthorizedAsync(HttpMethod.Delete, "/api/account", tokens.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var login = await Client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequestDto { Email = "delete.self@example.com", Password = "Passw0rd1" },
        TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
  }

  [Fact]
  public async Task DeleteSelf_AfterUserAlreadyDeleted_ReturnsNotFound()
  {
    var tokens = await RegisterAsync("delete.twice@example.com");

    var first = await SendAuthorizedAsync(HttpMethod.Delete, "/api/account", tokens.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

    var second = await SendAuthorizedAsync(HttpMethod.Delete, "/api/account", tokens.AccessToken);
    Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
  }
}

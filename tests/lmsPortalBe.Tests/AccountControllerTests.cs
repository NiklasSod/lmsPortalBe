using System.Net;
using System.Net.Http.Json;
using lmsPortalBe.DTOs.Admin;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.User;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

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

  private async Task<AuthResponseDto> CreateTeacherAsync(string email)
  {
    await RegisterAsync(email);

    var admin = await LoginAsync("admin@example.com", "AdminPass1");
    var promote = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/admin/assign-role",
        admin.AccessToken,
        new AssignRoleRequestDto { Email = email, Role = "teacher" });
    promote.EnsureSuccessStatusCode();

    return await LoginAsync(email, "Passw0rd1");
  }

  private async Task<ApplicationUser> GetUserAsync(string email)
  {
    using var scope = Factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync(email);
    Assert.NotNull(user);
    return user;
  }

  [Fact]
  public async Task UpdateSelf_WithoutToken_ReturnsUnauthorized()
  {
    var response = await Client.PutAsync(
        "/api/account",
        JsonContent.Create(new UpdateUserRequestDto
        {
          FirstName = "Alice",
          LastName = "Doe",
          Email = "alice@example.com"
        }),
        TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task UpdateSelf_AsStudent_UpdatesOwnNameAndEmail()
  {
    var student = await RegisterAsync("update.self.student@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        "/api/account",
        student.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "New",
          LastName = "Name",
          Email = "update.self.new@example.com"
        });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var user = await GetUserAsync("update.self.new@example.com");
    Assert.Equal("New", user.FirstName);
    Assert.Equal("Name", user.LastName);

    var login = await Client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequestDto { Email = "update.self.new@example.com", Password = "Passw0rd1" },
        TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.OK, login.StatusCode);
  }

  [Fact]
  public async Task UpdateSelf_AsTeacher_UpdatesOwnName()
  {
    var teacher = await CreateTeacherAsync("update.self.teacher@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        "/api/account",
        teacher.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "Teacher",
          LastName = "Updated",
          Email = "update.self.teacher@example.com"
        });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var user = await GetUserAsync("update.self.teacher@example.com");
    Assert.Equal("Teacher", user.FirstName);
    Assert.Equal("Updated", user.LastName);
  }

  [Fact]
  public async Task UpdateSelf_DuplicateEmail_ReturnsBadRequest()
  {
    var student = await RegisterAsync("update.self.dupe.one@example.com");
    await RegisterAsync("update.self.dupe.two@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        "/api/account",
        student.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "Dup",
          LastName = "User",
          Email = "update.self.dupe.two@example.com"
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpdateSelf_InvalidEmail_ReturnsBadRequest()
  {
    var student = await RegisterAsync("update.self.invalid@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        "/api/account",
        student.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "Bad",
          LastName = "Email",
          Email = "not-an-email"
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task ChangePassword_WithoutToken_ReturnsUnauthorized()
  {
    var response = await Client.PostAsync(
        "/api/account/change-password",
        JsonContent.Create(new ChangePasswordRequestDto
        {
          CurrentPassword = "Passw0rd1",
          NewPassword = "NewPassw0rd1"
        }),
        TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task ChangePassword_AsStudent_SucceedsAndOldPasswordFails()
  {
    var student = await RegisterAsync("change.pw.student@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/account/change-password",
        student.AccessToken,
        new ChangePasswordRequestDto
        {
          CurrentPassword = "Passw0rd1",
          NewPassword = "NewPassw0rd1"
        });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var oldLogin = await Client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequestDto { Email = "change.pw.student@example.com", Password = "Passw0rd1" },
        TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

    var newLogin = await Client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequestDto { Email = "change.pw.student@example.com", Password = "NewPassw0rd1" },
        TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
  }

  [Fact]
  public async Task ChangePassword_AsTeacher_Succeeds()
  {
    var teacher = await CreateTeacherAsync("change.pw.teacher@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/account/change-password",
        teacher.AccessToken,
        new ChangePasswordRequestDto
        {
          CurrentPassword = "Passw0rd1",
          NewPassword = "NewPassw0rd1"
        });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
  {
    var student = await RegisterAsync("change.pw.wrong@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/account/change-password",
        student.AccessToken,
        new ChangePasswordRequestDto
        {
          CurrentPassword = "WrongPassw0rd1",
          NewPassword = "NewPassw0rd1"
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task ChangePassword_WeakNewPassword_ReturnsBadRequest()
  {
    var student = await RegisterAsync("change.pw.weak@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/account/change-password",
        student.AccessToken,
        new ChangePasswordRequestDto
        {
          CurrentPassword = "Passw0rd1",
          NewPassword = "short"
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }
}

using System.Net;
using lmsPortalBe.DTOs.Admin;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.User;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace lmsPortalBe.Tests;

public class UserControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public UserControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  private Task<AuthResponseDto> LoginAsAdminAsync()
      => LoginAsync("admin@example.com", "AdminPass1");

  private async Task<AuthResponseDto> CreateTeacherAsync(string email)
  {
    await RegisterAsync(email);

    var admin = await LoginAsAdminAsync();
    var promote = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/admin/assign-role",
        admin.AccessToken,
        new AssignRoleRequestDto { Email = email, Role = "teacher" });
    promote.EnsureSuccessStatusCode();

    return await LoginAsync(email, "Passw0rd1");
  }

  private async Task<string> GetUserIdAsync(string email)
  {
    using var scope = Factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync(email);
    Assert.NotNull(user);
    return user.Id;
  }

  [Fact]
  public async Task UpdateUser_AsAdmin_UpdatesStudentNamesAndEmail()
  {
    await RegisterAsync("update.student@example.com");
    var userId = await GetUserIdAsync("update.student@example.com");
    var admin = await LoginAsAdminAsync();

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        $"/api/users/{userId}",
        admin.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "Alice",
          LastName = "Smith",
          Email = "alice.smith@example.com"
        });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    using var scope = Factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync("alice.smith@example.com");
    Assert.NotNull(user);
    Assert.Equal("Alice", user.FirstName);
    Assert.Equal("Smith", user.LastName);
  }

  [Fact]
  public async Task UpdateUser_AsTeacher_UpdatesStudent()
  {
    await RegisterAsync("update.byteacher@example.com");
    var teacher = await CreateTeacherAsync("update.teacher@example.com");
    var userId = await GetUserIdAsync("update.byteacher@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        $"/api/users/{userId}",
        teacher.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "Bob",
          LastName = "Brown",
          Email = "update.byteacher@example.com"
        });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task UpdateUser_AsStudent_ReturnsForbidden()
  {
    var student = await RegisterAsync("update.forbidden@example.com");
    await RegisterAsync("update.target@example.com");
    var targetId = await GetUserIdAsync("update.target@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        $"/api/users/{targetId}",
        student.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "X",
          LastName = "Y",
          Email = "update.target@example.com"
        });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task UpdateUser_OnAdministrator_ReturnsBadRequest()
  {
    var admin = await LoginAsAdminAsync();
    var adminId = await GetUserIdAsync("admin@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        $"/api/users/{adminId}",
        admin.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "Root",
          LastName = "User",
          Email = "admin@example.com"
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpdateUser_UnknownUser_ReturnsNotFound()
  {
    var admin = await LoginAsAdminAsync();

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        "/api/users/nonexistent-id",
        admin.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "No",
          LastName = "Body",
          Email = "nobody@example.com"
        });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task UpdateUser_DuplicateEmail_ReturnsBadRequest()
  {
    await RegisterAsync("update.dupe.one@example.com");
    await RegisterAsync("update.dupe.two@example.com");

    var admin = await LoginAsAdminAsync();
    var userId = await GetUserIdAsync("update.dupe.two@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Put,
        $"/api/users/{userId}",
        admin.AccessToken,
        new UpdateUserRequestDto
        {
          FirstName = "Dup",
          LastName = "User",
          Email = "update.dupe.one@example.com"
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }
}

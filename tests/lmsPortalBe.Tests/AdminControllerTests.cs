using System.Net;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.Admin;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace lmsPortalBe.Tests;

public class AdminControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public AdminControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  private Task<AuthResponseDto> LoginAsAdminAsync()
      => LoginAsync("admin@example.com", "AdminPass1");

  private async Task<IList<string>> GetRolesAsync(string email)
  {
    using var scope = Factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync(email);
    Assert.NotNull(user);
    return await userManager.GetRolesAsync(user);
  }

  [Fact]
  public async Task AssignRole_AsAdmin_SwapsStudentForTeacher()
  {
    await RegisterAsync("assign.role@example.com");
    var admin = await LoginAsAdminAsync();

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/admin/assign-role",
        admin.AccessToken,
        new AssignRoleRequestDto { Email = "assign.role@example.com", Role = "teacher" });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var roles = await GetRolesAsync("assign.role@example.com");
    Assert.Contains("teacher", roles);
    Assert.DoesNotContain("student", roles);
  }

  [Fact]
  public async Task AssignRole_AsNonAdmin_ReturnsForbidden()
  {
    var student = await RegisterAsync("assign.forbidden@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/admin/assign-role",
        student.AccessToken,
        new AssignRoleRequestDto { Email = "assign.forbidden@example.com", Role = "teacher" });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task AssignRole_UnknownUser_ReturnsNotFound()
  {
    var admin = await LoginAsAdminAsync();

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/admin/assign-role",
        admin.AccessToken,
        new AssignRoleRequestDto { Email = "nobody@example.com", Role = "teacher" });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task AssignRole_ToAdministrator_ReturnsBadRequest()
  {
    var admin = await LoginAsAdminAsync();

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/admin/assign-role",
        admin.AccessToken,
        new AssignRoleRequestDto { Email = "admin@example.com", Role = "student" });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }
}

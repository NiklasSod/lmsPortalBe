using System.Net;
using System.Net.Http.Json;
using lmsPortalBe.DTOs.UserProfile;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace lmsPortalBe.Tests;

public class UserProfileControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public UserProfileControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  private async Task<string> GetUserIdAsync(string email)
  {
    using var scope = Factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync(email);
    Assert.NotNull(user);
    return user.Id;
  }

  private async Task<UserProfileDto> GetMyProfileAsync(string accessToken)
  {
    var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/profiles/me", accessToken);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<UserProfileDto>(TestContext.Current.CancellationToken))!;
  }

  [Fact]
  public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
  {
    var response = await Client.GetAsync("/api/profiles/me", TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetMyProfile_WhenMissing_ReturnsNotFound()
  {
    var student = await RegisterAsync("profile.missing@example.com");

    var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/profiles/me", student.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task CreateProfile_ThenGetMyProfile_ReturnsCreatedProfile()
  {
    var student = await RegisterAsync("profile.create@example.com");

    var createResponse = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/profiles",
        student.AccessToken,
        new CreateUserProfileRequestDto
        {
          AboutMe = "I like building APIs.",
          GitHubLink = "https://github.com/janedoe",
          Skills = new List<string> { "C#", "SQL" },
          DateOfBirth = new DateOnly(1999, 5, 4)
        });

    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

    var profile = await GetMyProfileAsync(student.AccessToken);
    Assert.Equal("I like building APIs.", profile.AboutMe);
    Assert.Equal("https://github.com/janedoe", profile.GitHubLink);
    Assert.Equal(new List<string> { "C#", "SQL" }, profile.Skills);
    Assert.Equal(new DateOnly(1999, 5, 4), profile.DateOfBirth);
  }

  [Fact]
  public async Task CreateProfile_WhenAlreadyExists_ReturnsConflict()
  {
    var student = await RegisterAsync("profile.conflict@example.com");

    var first = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/profiles",
        student.AccessToken,
        new CreateUserProfileRequestDto { AboutMe = "First" });
    first.EnsureSuccessStatusCode();

    var second = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/profiles",
        student.AccessToken,
        new CreateUserProfileRequestDto { AboutMe = "Second" });

    Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
  }

  [Fact]
  public async Task UpdateProfile_PartialUpdate_KeepsOtherFields()
  {
    var student = await RegisterAsync("profile.patch@example.com");

    var createResponse = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/profiles",
        student.AccessToken,
        new CreateUserProfileRequestDto
        {
          AboutMe = "Before",
          GitHubLink = "https://github.com/janedoe"
        });
    createResponse.EnsureSuccessStatusCode();

    var patchResponse = await SendAuthorizedAsync(
        HttpMethod.Patch,
        "/api/profiles",
        student.AccessToken,
        new UpdateUserProfileRequestDto { AboutMe = "After" });

    Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

    var profile = await GetMyProfileAsync(student.AccessToken);
    Assert.Equal("After", profile.AboutMe);
    Assert.Equal("https://github.com/janedoe", profile.GitHubLink);
  }

  [Fact]
  public async Task UpdateProfile_WhenMissing_ReturnsNotFound()
  {
    var student = await RegisterAsync("profile.patch.missing@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        "/api/profiles",
        student.AccessToken,
        new UpdateUserProfileRequestDto { AboutMe = "Should not apply" });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetProfile_OtherUser_ReturnsProfile()
  {
    var owner = await RegisterAsync("profile.owner@example.com");
    var viewer = await RegisterAsync("profile.viewer@example.com");

    var createResponse = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/profiles",
        owner.AccessToken,
        new CreateUserProfileRequestDto { AboutMe = "Visible to others" });
    createResponse.EnsureSuccessStatusCode();

    var ownerId = await GetUserIdAsync("profile.owner@example.com");
    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/profiles/{ownerId}",
        viewer.AccessToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(profile);
    Assert.Equal("Visible to others", profile.AboutMe);
  }

  [Fact]
  public async Task UpdateProfile_DoesNotAffectOtherUser()
  {
    var owner = await RegisterAsync("profile.owner2@example.com");
    var other = await RegisterAsync("profile.other2@example.com");

    var createOwner = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/profiles",
        owner.AccessToken,
        new CreateUserProfileRequestDto { AboutMe = "Owner original" });
    createOwner.EnsureSuccessStatusCode();

    var createOther = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/profiles",
        other.AccessToken,
        new CreateUserProfileRequestDto { AboutMe = "Other original" });
    createOther.EnsureSuccessStatusCode();

    // PATCH never takes a user id, so it can only touch the caller's own profile.
    var patchOther = await SendAuthorizedAsync(
        HttpMethod.Patch,
        "/api/profiles",
        other.AccessToken,
        new UpdateUserProfileRequestDto { AboutMe = "Other changed" });
    patchOther.EnsureSuccessStatusCode();

    var ownerId = await GetUserIdAsync("profile.owner2@example.com");
    var getOwner = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/profiles/{ownerId}",
        owner.AccessToken);

    var ownerProfile = await getOwner.Content.ReadFromJsonAsync<UserProfileDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(ownerProfile);
    Assert.Equal("Owner original", ownerProfile.AboutMe);
  }
}

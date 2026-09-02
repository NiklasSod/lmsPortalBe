using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using lmsPortalBe.DTOs.Admin;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.Course;

namespace lmsPortalBe.Tests;

public class ModuleControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public ModuleControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  private static readonly DateTime Jan1 = new(2026, 1, 1);
  private static readonly DateTime Jan31 = new(2026, 1, 31);
  private static readonly DateTime Jan15 = new(2026, 1, 15);
  private static readonly DateTime Feb15 = new(2026, 2, 15);
  private static readonly DateTime Feb1 = new(2026, 2, 1);
  private static readonly DateTime Feb28 = new(2026, 2, 28);

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

    // Re-login so the issued token carries the teacher role claim.
    return await LoginAsync(email, "Passw0rd1");
  }

  private async Task<int> CreateCourseAsync(string teacherToken, DateTime start, DateTime end)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/courses",
        teacherToken,
        new CreateCourseRequestDto
        {
          Name = $"Course {start:yyyy-MM-dd}",
          Description = "Test course",
          StartDate = start,
          EndDate = end
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<CourseSummaryDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  private async Task<int> CreateModuleAsync(string teacherToken, int courseId, DateTime start, DateTime end)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/modules",
        teacherToken,
        new CreateCourseModuleRequestDto
        {
          CourseId = courseId,
          Name = $"Course {start:yyyy-MM-dd}",
          Description = "Test course",
          StartDate = start,
          EndDate = end
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<CourseModuleSummaryDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  [Fact]
  public async Task CreateModule_AsTeacher_ReturnsCreatedWithId()
  {
    var teacher = await CreateTeacherAsync("course.teacher.create.module@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/modules",
        teacher.AccessToken,
        new CreateCourseModuleRequestDto
        {
          CourseId = courseId,
          Name = "Algebra",
          Description = "Intro to algebra",
          StartDate = Jan1,
          EndDate = Jan31
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<CourseModuleSummaryDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.NotEqual(0, body.Id);
    Assert.Equal("Algebra", body.Name);
  }

  [Fact]
  public async Task CreateModule_AsStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("course.teacher.not.forbidden@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);

    var student = await RegisterAsync("course.student.forbidden@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/modules",
        student.AccessToken,
        new CreateCourseModuleRequestDto
        {
          CourseId = courseId,
          Name = "Algebra",
          Description = "Intro to algebra",
          StartDate = Jan1,
          EndDate = Jan31
        });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateModule_WithEndBeforeStart_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("course.teacher.wrong.dates@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/modules",
        teacher.AccessToken,
        new CreateCourseModuleRequestDto
        {
          CourseId = courseId,
          Name = "Bad dates",
          Description = "Invalid",
          StartDate = Jan31,
          EndDate = Jan1
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task DeleteModule_AsCreator_ReturnsNoContent()
  {
    var teacher = await CreateTeacherAsync("course.teacher.delete@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/modules/{moduleId}",
        teacher.AccessToken);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var get = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/modules/{moduleId}",
        teacher.AccessToken);
    Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
  }

  [Fact]
  public async Task DeleteModule_AsNonCreatorTeacher_ReturnsForbidden()
  {
    var creator = await CreateTeacherAsync("course.teacher.delete.creator@example.com");
    var other = await CreateTeacherAsync("course.teacher.delete.other@example.com");

    var courseId = await CreateCourseAsync(creator.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(creator.AccessToken, courseId, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/modules/{moduleId}",
        other.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteModule_AsStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("course.teacher.delete.student@example.com");
    var student = await RegisterAsync("course.student.delete@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/modules/{moduleId}",
        student.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteModule_UnknownCourse_ReturnsNotFound()
  {
    var teacher = await CreateTeacherAsync("course.teacher.delete.missing@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        "/api/modules/999999",
        teacher.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task UpdateModule_AsTeacher_ReturnsOkAndUpdates()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/modules/{moduleId}",
        teacher.AccessToken,
        new UpdateCourseModuleRequestDto { Name = "Algebra II" });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<CourseModuleSummaryDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.Equal("Algebra II", body.Name);
    Assert.Equal("Test course", body.Description);
    Assert.Equal(Jan1, body.StartDate);
    Assert.Equal(Jan31, body.EndDate);
  }

  [Fact]
  public async Task UpdateModule_AsStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update.student@example.com");
    var student = await RegisterAsync("course.student.update@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/modules/{moduleId}",
        student.AccessToken,
        new UpdateCourseModuleRequestDto { Name = "Algebra II" });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task UpdateModule_WithStartAfterEnd_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update.dates@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/modules/{moduleId}",
        teacher.AccessToken,
        new UpdateCourseModuleRequestDto { StartDate = Feb28 });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpdateModule_UnknownCourse_ReturnsNotFound()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update.missing@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        "/api/modules/999999",
        teacher.AccessToken,
        new UpdateCourseModuleRequestDto { Name = "Missing" });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }
}

using System.Net;
using System.Net.Http.Json;
using lmsPortalBe.DTOs.Admin;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.Course;
using lmsPortalBe.DTOs.Resource;

namespace lmsPortalBe.Tests;

public class ResourceControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public ResourceControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  private static readonly DateTime Jan1 = new(2026, 1, 1);
  private static readonly DateTime Jan31 = new(2026, 1, 31);

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

  private async Task<int> CreateCourseAsync(string teacherToken)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/courses",
        teacherToken,
        new CreateCourseRequestDto
        {
          Name = "Resource test course",
          Description = "Course for resource tests",
          StartDate = Jan1,
          EndDate = Jan31
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<CourseSummaryDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  private async Task<int> CreateModuleAsync(string teacherToken, int courseId)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/modules",
        teacherToken,
        new CreateCourseModuleRequestDto
        {
          CourseId = courseId,
          Name = "Resource test module",
          Description = "Module for resource tests",
          StartDate = Jan1,
          EndDate = Jan31
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<CourseModuleSummaryDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  private async Task<int> CreateActivityAsync(string teacherToken, int moduleId)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/activities",
        teacherToken,
        new CreateActivityRequestDto
        {
          ModuleId = moduleId,
          Type = "Lecture",
          Name = "Resource test activity",
          Description = "Activity for resource tests",
          StartDate = Jan1,
          EndDate = Jan31
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ActivityDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  private async Task<int> CreateResourceAsync(string teacherToken, CreateResourceRequestDto dto)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/resources",
        teacherToken,
        dto);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ResourceDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  private async Task EnrollAsync(string token, int courseId)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/courses/enroll",
        token,
        new EnrollRequestDto { CourseId = courseId });
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task CreateResource_UnderCourse_AsTeacher_ReturnsCreatedWithId()
  {
    var teacher = await CreateTeacherAsync("resource.create.course@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/resources",
        teacher.AccessToken,
        new CreateResourceRequestDto
        {
          DisplayName = "Slides",
          Url = "https://example.com/slides.pdf",
          CourseId = courseId
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ResourceDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.NotEqual(0, body.Id);
    Assert.Equal("Slides", body.DisplayName);
    Assert.Equal(courseId, body.CourseId);
  }

  [Fact]
  public async Task CreateResource_UnderModule_AsTeacher_ReturnsCreatedWithId()
  {
    var teacher = await CreateTeacherAsync("resource.create.module@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/resources",
        teacher.AccessToken,
        new CreateResourceRequestDto
        {
          DisplayName = "Reading",
          Url = "https://example.com/reading.pdf",
          ModuleId = moduleId
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ResourceDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.NotEqual(0, body.Id);
    Assert.Equal(moduleId, body.ModuleId);
  }

  [Fact]
  public async Task CreateResource_UnderActivity_AsTeacher_ReturnsCreatedWithId()
  {
    var teacher = await CreateTeacherAsync("resource.create.activity@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var activityId = await CreateActivityAsync(teacher.AccessToken, moduleId);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/resources",
        teacher.AccessToken,
        new CreateResourceRequestDto
        {
          DisplayName = "Lab handout",
          Url = "https://example.com/lab.pdf",
          ActivityId = activityId
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ResourceDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.NotEqual(0, body.Id);
    Assert.Equal(activityId, body.ActivityId);
  }

  [Fact]
  public async Task CreateResource_WithNoParent_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("resource.create.noparent@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/resources",
        teacher.AccessToken,
        new CreateResourceRequestDto
        {
          DisplayName = "Orphan",
          Url = "https://example.com/orphan.pdf"
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateResource_WithTwoParents_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("resource.create.twoparents@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/resources",
        teacher.AccessToken,
        new CreateResourceRequestDto
        {
          DisplayName = "Confused",
          Url = "https://example.com/confused.pdf",
          CourseId = courseId,
          ModuleId = moduleId
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetCourseResources_AsAdmin_ReturnsResources()
  {
    var teacher = await CreateTeacherAsync("resource.list.course.admin@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var resourceId = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Slides",
      Url = "https://example.com/slides.pdf",
      CourseId = courseId
    });

    var admin = await LoginAsync("admin@example.com", "AdminPass1");
    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/courses/{courseId}/resources",
        admin.AccessToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(resources);
    Assert.Contains(resources, r => r.Id == resourceId);
  }

  [Fact]
  public async Task GetCourseResources_AsEnrolledStudent_ReturnsResources()
  {
    var teacher = await CreateTeacherAsync("resource.list.course.enrolled@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var resourceId = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Slides",
      Url = "https://example.com/slides.pdf",
      CourseId = courseId
    });

    var student = await RegisterAsync("resource.student.enrolled@example.com");
    await EnrollAsync(student.AccessToken, courseId);

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/courses/{courseId}/resources",
        student.AccessToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(resources);
    Assert.Contains(resources, r => r.Id == resourceId);
  }

  [Fact]
  public async Task GetCourseResources_AsNonEnrolledStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("resource.list.course.foreign@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Slides",
      Url = "https://example.com/slides.pdf",
      CourseId = courseId
    });

    var outsider = await RegisterAsync("resource.student.foreign@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/courses/{courseId}/resources",
        outsider.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task GetModuleResources_AsTeacher_ReturnsResources()
  {
    var teacher = await CreateTeacherAsync("resource.list.module@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var resourceId = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Reading",
      Url = "https://example.com/reading.pdf",
      ModuleId = moduleId
    });

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/modules/{moduleId}/resources",
        teacher.AccessToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(resources);
    Assert.Contains(resources, r => r.Id == resourceId);
  }

  [Fact]
  public async Task GetActivityResources_AsTeacher_ReturnsResources()
  {
    var teacher = await CreateTeacherAsync("resource.list.activity@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var activityId = await CreateActivityAsync(teacher.AccessToken, moduleId);
    var resourceId = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Lab handout",
      Url = "https://example.com/lab.pdf",
      ActivityId = activityId
    });

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/activity/{activityId}/resources",
        teacher.AccessToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(resources);
    Assert.Contains(resources, r => r.Id == resourceId);
  }

  [Fact]
  public async Task GetCourseResources_UnknownCourse_ReturnsNotFound()
  {
    var teacher = await CreateTeacherAsync("resource.list.course.missing@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        "/api/courses/999999/resources",
        teacher.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task DeleteCourse_CascadeDeletesResources()
  {
    var teacher = await CreateTeacherAsync("resource.delete.course@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var resourceId = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Slides",
      Url = "https://example.com/slides.pdf",
      CourseId = courseId
    });

    var delete = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/courses/{courseId}",
        teacher.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

    var admin = await LoginAsync("admin@example.com", "AdminPass1");
    var get = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/resources/{resourceId}",
        admin.AccessToken);
    Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
  }

  [Fact]
  public async Task DeleteModule_CascadeDeletesResources()
  {
    var teacher = await CreateTeacherAsync("resource.delete.module@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var resourceId = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Reading",
      Url = "https://example.com/reading.pdf",
      ModuleId = moduleId
    });

    var delete = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/modules/{moduleId}",
        teacher.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

    var admin = await LoginAsync("admin@example.com", "AdminPass1");
    var get = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/resources/{resourceId}",
        admin.AccessToken);
    Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
  }

  [Fact]
  public async Task DeleteActivity_CascadeDeletesResources()
  {
    var teacher = await CreateTeacherAsync("resource.delete.activity@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var activityId = await CreateActivityAsync(teacher.AccessToken, moduleId);
    var resourceId = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Lab handout",
      Url = "https://example.com/lab.pdf",
      ActivityId = activityId
    });

    var delete = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/activities/{activityId}",
        teacher.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

    var admin = await LoginAsync("admin@example.com", "AdminPass1");
    var get = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/resources/{resourceId}",
        admin.AccessToken);
    Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
  }
}

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using lmsPortalBe.DTOs.Admin;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.Course;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace lmsPortalBe.Tests;

public class SubmissionsControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public SubmissionsControllerTests(TestWebApplicationFactory factory) : base(factory)
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

  private async Task<int> CreateAssignmentAsync(string teacherToken, int moduleId, DateTime start)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/assignments",
        teacherToken,
        new CreateAssignmentRequestDto
        {
          ModuleId = moduleId,
          Name = $"Course {start:yyyy-MM-dd}",
          Description = "Test course",
          DueDate = start,
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<AssignmentDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  private async Task<string> GetUserIdAsync(string email)
  {
    using var scope = Factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync(email);
    Assert.NotNull(user);
    return user.Id;
  }


// TODO: does not create anything at the moment because of bad request
  private async Task<int> CreateSubmissionAsync(string studentToken, string studentId, int assignmentId, DateTime? handinDate)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/submissions",
        studentToken,
        new CreateSubmissionRequestDto
        {
          StudentId = studentId,
          AssignmentId = assignmentId,
          Content = "Test content",
          HandinDate = handinDate,
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  [Fact]
  public async Task CreateSubmission_AsTeacher_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("submission.teacher.create.module@example.com");
    var teacherId = await GetUserIdAsync("submission.teacher.create.module@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan15);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/submissions",
        teacher.AccessToken,
        new CreateSubmissionRequestDto
        {
          StudentId = teacherId,
          AssignmentId = assignmentId,
          Content = "Example content from teacher."
        });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateSubmission_AsStudent_ReturnsCreatedWithId()
  {
    var student = await RegisterAsync("create.submission.student@example.com");
    var studentId = await GetUserIdAsync("create.submission.student");
    // TODO: enroll student here
    var teacher = await CreateTeacherAsync("course.teacher.not.forbidden@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan15);


    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/assignments",
        student.AccessToken,
        new CreateSubmissionRequestDto
        {
          AssignmentId = assignmentId,
          StudentId = studentId,
          Content = "Student's finest work."
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.NotEqual(0, body.Id);
    Assert.Equal("Student's finest work.", body.Content);
  }

  [Fact]
  public async Task CreateSubmission_AsNonEnrolledStudent_ReturnsForbidden()
  {
    var student = await RegisterAsync("create.submission.student@example.com");
    var studentId = await GetUserIdAsync("create.submission.student");
    var teacher = await CreateTeacherAsync("course.teacher.not.forbidden@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan15);


    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/assignments",
        student.AccessToken,
        new CreateSubmissionRequestDto
        {
          AssignmentId = assignmentId,
          StudentId = studentId,
          Content = "Student's finest work."
        });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  

  [Fact]
  public async Task GetAllAssignment_AsAdmin_ReturnsAllAssignment()
  {
    var teacherA = await CreateTeacherAsync("course.teacher.list.a@example.com");
    var teacherB = await CreateTeacherAsync("course.teacher.list.b@example.com");

    var courseAId = await CreateCourseAsync(teacherA.AccessToken, Jan1, Jan31);
    var moduleAId = await CreateModuleAsync(teacherA.AccessToken, courseAId, Jan1, Jan31);
    var assignmentAId = await CreateAssignmentAsync(teacherA.AccessToken, moduleAId, Jan31);

    var courseBId = await CreateCourseAsync(teacherB.AccessToken, Jan1, Jan31);
    var moduleBId = await CreateModuleAsync(teacherB.AccessToken, courseBId, Jan1, Jan31);
    var assignmentBId = await CreateAssignmentAsync(teacherB.AccessToken, moduleBId, Jan31);

    var admin = await LoginAsync("admin@example.com", "AdminPass1");

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        "/api/assignments",
        admin.AccessToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var assignments = await response.Content.ReadFromJsonAsync<List<AssignmentDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(assignments);
    Assert.Contains(assignments, a => a.Id == assignmentAId);
    Assert.Contains(assignments, a => a.Id == assignmentBId);
  }

  [Fact]
  public async Task GetAllAssignments_AsTeacher_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("course.teacher.list.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        "/api/assignments",
        teacher.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreateAssignment_WithDueDateBeforeModuleStart_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("course.teacher.wrong.dates@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan15, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan15, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/assignments",
        teacher.AccessToken,
        new CreateAssignmentRequestDto
        {
          ModuleId = moduleId,
          Name = "Bad dates",
          Description = "Invalid",
          DueDate = Jan1
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateAssignment_WithDueDateAfterModuleEnd_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("course.teacher.wrong.dates.again@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan15);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan15);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/assignments",
        teacher.AccessToken,
        new CreateAssignmentRequestDto
        {
          ModuleId = moduleId,
          Name = "Bad dates",
          Description = "Invalid",
          DueDate = Jan31
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task DeleteAssignment_AsCreator_ReturnsNoContent()
  {
    var teacher = await CreateTeacherAsync("course.teacher.delete@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/assignments/{assignmentId}",
        teacher.AccessToken);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var get = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/assignments/{assignmentId}",
        teacher.AccessToken);
    Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
  }

  [Fact]
  public async Task DeleteAssignment_AsNonCreatorTeacher_ReturnsForbidden()
  {
    var creator = await CreateTeacherAsync("course.teacher.delete.creator@example.com");
    var other = await CreateTeacherAsync("course.teacher.delete.other@example.com");

    var courseId = await CreateCourseAsync(creator.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(creator.AccessToken, courseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(creator.AccessToken, moduleId, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/assignments/{assignmentId}",
        other.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteAssignment_AsStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("course.teacher.delete.student@example.com");
    var student = await RegisterAsync("course.student.delete@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    var assignmentyId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/assignments/{assignmentyId}",
        student.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteAssignment_UnknownCourse_ReturnsNotFound()
  {
    var teacher = await CreateTeacherAsync("course.teacher.delete.missing@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        "/api/assignments/999999",
        teacher.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task UpdateAssignment_AsTeacher_ReturnsOkAndUpdates()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/assignments/{assignmentId}",
        teacher.AccessToken,
        new UpdateAssignmentRequestDto { Name = "Algebra II" });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<AssignmentDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.Equal("Algebra II", body.Name);
    Assert.Equal("Test course", body.Description);
    Assert.Equal(Jan31, body.DueDate);
  }

  [Fact]
  public async Task UpdateAssignment_AsStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update.student@example.com");
    var student = await RegisterAsync("course.student.update@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/assignments/{assignmentId}",
        student.AccessToken,
        new UpdateAssignmentRequestDto { Name = "Algebra II" });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task UpdateAssignment_MoveToOtherModule_ByNonOwnerOfSource_ReturnsForbidden()
  {
    var owner = await CreateTeacherAsync("course.teacher.move.owner@example.com");
    var otherTeacher = await CreateTeacherAsync("course.teacher.move.other@example.com");

    var sourceCourseId = await CreateCourseAsync(owner.AccessToken, Jan1, Jan31);
    var sourceModuleId = await CreateModuleAsync(owner.AccessToken, sourceCourseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(owner.AccessToken, sourceModuleId, Jan31);

    var targetCourseId = await CreateCourseAsync(otherTeacher.AccessToken, Jan1, Jan31);
    var targetModuleId = await CreateModuleAsync(otherTeacher.AccessToken, targetCourseId, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/assignments/{assignmentId}",
        otherTeacher.AccessToken,
        new UpdateAssignmentRequestDto { ModuleId = targetModuleId, Name = "Stolen module" });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task UpdateAssignment_MoveToOwnedModule_ReturnsOkAndMoves()
  {
    var teacher = await CreateTeacherAsync("course.teacher.move.owned@example.com");

    var sourceCourseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var sourceModuleId = await CreateModuleAsync(teacher.AccessToken, sourceCourseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, sourceModuleId, Jan31);

    var targetCourseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var targetModuleId = await CreateModuleAsync(teacher.AccessToken, targetCourseId, Jan1, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/assignments/{assignmentId}",
        teacher.AccessToken,
        new UpdateAssignmentRequestDto { ModuleId = targetModuleId });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<AssignmentDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.Equal(targetModuleId, body.ModuleId);
  }

  [Fact]
  public async Task UpdateAssignment_WithDueDateAfterModuleEnd_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update.start@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan1, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan1, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/assignments/{assignmentId}",
        teacher.AccessToken,
        new UpdateAssignmentRequestDto { DueDate = Feb28 });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }


  [Fact]
  public async Task UpdateAssignment_WithDueDateBeforeModuleStart_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update.end@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken, Jan15, Jan31);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, Jan15, Jan31);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, Jan31);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/assignments/{assignmentId}",
        teacher.AccessToken,
        new UpdateAssignmentRequestDto { DueDate = Jan1 });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpdateAssignment_UnknownCourse_ReturnsNotFound()
  {
    var teacher = await CreateTeacherAsync("course.teacher.update.missing@example.com");

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        "/api/assignments/999999",
        teacher.AccessToken,
        new UpdateAssignmentRequestDto { Name = "Missing" });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetCurrentAssignments_AsStudent_ReturnsOnlyOpenAssignments()
  {
    var teacher = await CreateTeacherAsync("course.teacher.current@example.com");

    var now = DateTime.UtcNow;
    var courseId = await CreateCourseAsync(teacher.AccessToken, now.AddDays(-10), now.AddDays(10));
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId, now.AddDays(-10), now.AddDays(10));

    var pastAssignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, now.AddDays(-5));
    var openAssignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId, now.AddDays(5));

    var student = await RegisterAsync("course.student.current@example.com");
    var enroll = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/courses/enroll",
        student.AccessToken,
        new EnrollRequestDto { CourseId = courseId });
    enroll.EnsureSuccessStatusCode();

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        "/api/assignments/current",
        student.AccessToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var assignments = await response.Content.ReadFromJsonAsync<List<AssignmentDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(assignments);
    Assert.Contains(assignments, a => a.Id == openAssignmentId);
    Assert.DoesNotContain(assignments, a => a.Id == pastAssignmentId);
  }
}

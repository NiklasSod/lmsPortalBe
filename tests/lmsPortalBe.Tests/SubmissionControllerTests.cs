using System.Net;
using System.Net.Http.Json;
using lmsPortalBe.DTOs.Admin;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.Course;

namespace lmsPortalBe.Tests;

/// <summary>
/// Integration tests for the submission workflow: a student hands in work for
/// an assignment, the course teacher grades it (Approved / Revision), and the
/// submission can go back and forth several times. Each hand-in creates a new
/// history row.
/// </summary>
public class SubmissionsControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public SubmissionsControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  private static readonly DateTime Start = new(2027, 3, 1);
  private static readonly DateTime End = new(2027, 3, 31);
  private static readonly DateTime Due = new(2027, 3, 20);

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

  private async Task<AuthResponseDto> CreateStudentAsync(string email)
  {
    // Registration automatically assigns the "student" role.
    return await RegisterAsync(email);
  }

  private async Task<int> CreateCourseAsync(string teacherToken)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/courses",
        teacherToken,
        new CreateCourseRequestDto
        {
          Name = "Submission test course",
          Description = "Test course",
          StartDate = Start,
          EndDate = End
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
          Name = "Submission test module",
          Description = "Test module",
          StartDate = Start,
          EndDate = End
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<CourseModuleSummaryDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  private async Task<int> CreateAssignmentAsync(string teacherToken, int moduleId)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/assignments",
        teacherToken,
        new CreateAssignmentRequestDto
        {
          ModuleId = moduleId,
          Name = "Essay",
          Description = "Write an essay",
          DueDate = Due
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<AssignmentDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Id;
  }

  private async Task EnrollStudentAsync(string studentToken, int courseId)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/courses/enroll",
        studentToken,
        new EnrollRequestDto { CourseId = courseId });
    response.EnsureSuccessStatusCode();
  }

  private async Task<HttpResponseMessage> HandInAsync(string studentToken, int assignmentId, string content)
      => await SendAuthorizedAsync(
          HttpMethod.Post,
          "/api/submissions",
          studentToken,
          new CreateSubmissionRequestDto { AssignmentId = assignmentId, Content = content });

  private async Task<SubmissionDto?> GradeAsync(string teacherToken, int submissionId, string status, string? feedback)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/submissions/{submissionId}",
        teacherToken,
        new UpdateSubmissionRequestDto { Status = status, Feedback = feedback });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    return await response.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
  }

  [Fact]
  public async Task HandInSubmission_AsEnrolledStudent_ReturnsCreatedHandedIn()
  {
    var teacher = await CreateTeacherAsync("sub.enrolled.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.enrolled.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var response = await HandInAsync(student.AccessToken, assignmentId, "My first draft answer.");

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    Assert.NotEqual(0, body.Id);
    Assert.Equal(assignmentId, body.AssignmentId!.Value);
    Assert.Equal("My first draft answer.", body.Content);
    Assert.Equal("HandedIn", body.Status);
    Assert.NotEmpty(body.StudentId);
  }

  [Fact]
  public async Task HandInSubmission_WithNonexistentAssignment_ReturnsNotFound()
  {
    var student = await CreateStudentAsync("sub.noassignment.student@example.com");

    var response = await HandInAsync(student.AccessToken, 999999, "Answer to nothing.");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task HandInSubmission_WithZeroAssignmentId_ReturnsBadRequest()
  {
    var student = await CreateStudentAsync("sub.zeroassignment.student@example.com");

    var response = await HandInAsync(student.AccessToken, 0, "Answer.");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task HandInSubmission_UnenrolledStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("sub.unenrolled.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    // Registered student but never enrolled in the course.
    var student = await CreateStudentAsync("sub.unenrolled.student@example.com");

    var response = await HandInAsync(student.AccessToken, assignmentId, "Sneaky answer.");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task HandInSubmission_AsAdmin_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("sub.admin.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    // Admins are not in the "student" role, so they cannot hand in work.
    var admin = await LoginAsync("admin@example.com", "AdminPass1");

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/submissions",
        admin.AccessToken,
        new CreateSubmissionRequestDto { AssignmentId = assignmentId, Content = "Admin answer." });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task HandInSubmission_AfterApproved_ReturnsConflict()
  {
    var teacher = await CreateTeacherAsync("sub.conflict.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.conflict.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var handIn = await HandInAsync(student.AccessToken, assignmentId, "First submission.");
    Assert.Equal(HttpStatusCode.Created, handIn.StatusCode);
    var submission = await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submission);

    await GradeAsync(teacher.AccessToken, submission.Id, "Approved", "Accepted, well done!");

    var secondHandIn = await HandInAsync(student.AccessToken, assignmentId, "A second attempt after approval.");
    Assert.Equal(HttpStatusCode.Conflict, secondHandIn.StatusCode);
  }

  [Fact]
  public async Task RevisionThenResubmit_CreatesHistoryRows()
  {
    var teacher = await CreateTeacherAsync("sub.revision.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.revision.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var first = await HandInAsync(student.AccessToken, assignmentId, "Draft attempt.");
    Assert.Equal(HttpStatusCode.Created, first.StatusCode);
    var firstBody = await first.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(firstBody);

    // Teacher returns it for revision.
    var graded = await GradeAsync(teacher.AccessToken, firstBody.Id, "Revision", "Needs more sources.");
    Assert.NotNull(graded);
    Assert.Equal("Revision", graded.Status);

    // Student hands in again -> a new history row, back to HandedIn.
    var second = await HandInAsync(student.AccessToken, assignmentId, "Improved attempt.");
    Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    var secondBody = await second.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(secondBody);
    Assert.NotEqual(firstBody.Id, secondBody.Id);
    Assert.Equal("HandedIn", secondBody.Status);

    // The teacher can see both rows for the assignment.
    var listResponse = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/assignments/{assignmentId}/submissions",
        teacher.AccessToken);
    Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    var list = await listResponse.Content.ReadFromJsonAsync<List<SubmissionDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(list);
    Assert.Equal(2, list.Count);
    Assert.Contains(list, s => s.Id == firstBody.Id && s.Status == "Revision");
    Assert.Contains(list, s => s.Id == secondBody.Id && s.Status == "HandedIn");
  }

  [Fact]
  public async Task GradeSubmission_AsCourseTeacher_ApprovesWithFeedback()
  {
    var teacher = await CreateTeacherAsync("sub.grade.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.grade.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var handIn = await HandInAsync(student.AccessToken, assignmentId, "Final answer.");
    var submission = await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submission);

    var graded = await GradeAsync(teacher.AccessToken, submission.Id, "Approved", "Well done!");
    Assert.NotNull(graded);
    Assert.Equal("Approved", graded.Status);
    Assert.Equal("Well done!", graded.Feedback);

    // The owning student can read the graded submission.
    var getResponse = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/submissions/{submission.Id}",
        student.AccessToken);
    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    var viewed = await getResponse.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(viewed);
    Assert.Equal("Approved", viewed.Status);
  }

  [Fact]
  public async Task GradeSubmission_TeacherNotInCourse_ReturnsForbidden()
  {
    var teacherA = await CreateTeacherAsync("sub.other.teacher.a@example.com");
    var courseId = await CreateCourseAsync(teacherA.AccessToken);
    var moduleId = await CreateModuleAsync(teacherA.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacherA.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.other.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var handIn = await HandInAsync(student.AccessToken, assignmentId, "Answer.");
    var submission = await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submission);

    // A teacher with no relation to the course tries to grade.
    var teacherB = await CreateTeacherAsync("sub.other.teacher.b@example.com");
    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/submissions/{submission.Id}",
        teacherB.AccessToken,
        new UpdateSubmissionRequestDto { Status = "Approved" });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task GradeSubmission_InvalidStatus_ReturnsBadRequest()
  {
    var teacher = await CreateTeacherAsync("sub.invalid.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.invalid.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var handIn = await HandInAsync(student.AccessToken, assignmentId, "Answer.");
    var submission = await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submission);

    var response = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/submissions/{submission.Id}",
        teacher.AccessToken,
        new UpdateSubmissionRequestDto { Status = "Graded" });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetSubmission_AnotherStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("sub.privacy.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var studentA = await CreateStudentAsync("sub.privacy.student.a@example.com");
    await EnrollStudentAsync(studentA.AccessToken, courseId);
    var handIn = await HandInAsync(studentA.AccessToken, assignmentId, "Student A's answer.");
    var submission = await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submission);

    // A classmate must not be able to read Student A's submission.
    var studentB = await CreateStudentAsync("sub.privacy.student.b@example.com");
    await EnrollStudentAsync(studentB.AccessToken, courseId);

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/submissions/{submission.Id}",
        studentB.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task GetMySubmissions_ReturnsOnlyOwn()
  {
    var teacher = await CreateTeacherAsync("sub.mine.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var studentA = await CreateStudentAsync("sub.mine.student.a@example.com");
    await EnrollStudentAsync(studentA.AccessToken, courseId);
    var handInA = await HandInAsync(studentA.AccessToken, assignmentId, "Answer by A.");
    var submissionA = await handInA.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submissionA);

    var studentB = await CreateStudentAsync("sub.mine.student.b@example.com");
    await EnrollStudentAsync(studentB.AccessToken, courseId);
    var handInB = await HandInAsync(studentB.AccessToken, assignmentId, "Answer by B.");
    var submissionB = await handInB.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submissionB);

    var responseA = await SendAuthorizedAsync(HttpMethod.Get, "/api/submissions/mine", studentA.AccessToken);
    Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
    var mineA = await responseA.Content.ReadFromJsonAsync<List<SubmissionDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(mineA);
    Assert.Single(mineA);
    Assert.Equal(submissionA.Id, mineA[0].Id);

    var responseB = await SendAuthorizedAsync(HttpMethod.Get, "/api/submissions/mine", studentB.AccessToken);
    var mineB = await responseB.Content.ReadFromJsonAsync<List<SubmissionDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(mineB);
    Assert.Single(mineB);
    Assert.Equal(submissionB.Id, mineB[0].Id);
  }

  [Fact]
  public async Task GetAssignmentSubmissions_AsStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("sub.list.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.list.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var response = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/assignments/{assignmentId}/submissions",
        student.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteSubmission_AsOwnerStudent_ReturnsNoContent()
  {
    var teacher = await CreateTeacherAsync("sub.delete.owner.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.delete.owner.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var handIn = await HandInAsync(student.AccessToken, assignmentId, "Answer.");
    var submission = await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submission);

    var delete = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/submissions/{submission.Id}",
        student.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

    var get = await SendAuthorizedAsync(
        HttpMethod.Get,
        $"/api/submissions/{submission.Id}",
        student.AccessToken);
    Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
  }

  [Fact]
  public async Task DeleteSubmission_AnotherStudent_ReturnsForbidden()
  {
    var teacher = await CreateTeacherAsync("sub.delete.other.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var studentA = await CreateStudentAsync("sub.delete.other.student.a@example.com");
    await EnrollStudentAsync(studentA.AccessToken, courseId);
    var handIn = await HandInAsync(studentA.AccessToken, assignmentId, "Answer by A.");
    var submission = await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submission);

    var studentB = await CreateStudentAsync("sub.delete.other.student.b@example.com");
    await EnrollStudentAsync(studentB.AccessToken, courseId);

    var response = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/submissions/{submission.Id}",
        studentB.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteAssignment_KeepsStudentSubmissionAsHistory()
  {
    var teacher = await CreateTeacherAsync("sub.keep.teacher@example.com");
    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);

    var student = await CreateStudentAsync("sub.keep.student@example.com");
    await EnrollStudentAsync(student.AccessToken, courseId);

    var handIn = await HandInAsync(student.AccessToken, assignmentId, "My work that must survive.");
    var submission = await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken);
    Assert.NotNull(submission);
    Assert.Equal(assignmentId, submission.AssignmentId!.Value);

    var delete = await SendAuthorizedAsync(
        HttpMethod.Delete,
        $"/api/assignments/{assignmentId}",
        teacher.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

    // The student's work is kept as history, now detached from the deleted assignment.
    var mineResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/submissions/mine", student.AccessToken);
    Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
    var mine = await mineResponse.Content.ReadFromJsonAsync<List<SubmissionDto>>(TestContext.Current.CancellationToken);
    Assert.NotNull(mine);
    var kept = Assert.Single(mine);
    Assert.Equal(submission.Id, kept.Id);
    Assert.Equal("My work that must survive.", kept.Content);
    Assert.Null(kept.AssignmentId);
  }
}

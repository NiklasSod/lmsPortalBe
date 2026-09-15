using System.Net;
using System.Net.Http.Json;
using lmsPortalBe.DTOs.Admin;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.Course;
using lmsPortalBe.DTOs.Notification;
using lmsPortalBe.DTOs.Resource;

namespace lmsPortalBe.Tests;

public class NotificationsControllerTests : ApiTestBase, IClassFixture<TestWebApplicationFactory>
{
  public NotificationsControllerTests(TestWebApplicationFactory factory) : base(factory)
  {
  }

  private static readonly DateTime Start = new(2027, 4, 1);
  private static readonly DateTime End = new(2027, 4, 30);
  private static readonly DateTime Due = new(2027, 4, 15);

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

  private Task<AuthResponseDto> CreateStudentAsync(string email) => RegisterAsync(email);

  private async Task<int> CreateCourseAsync(string teacherToken)
  {
    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/courses",
        teacherToken,
        new CreateCourseRequestDto
        {
          Name = "Notification test course",
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
          Name = "Notification test module",
          Description = "Test module",
          StartDate = Start,
          EndDate = End
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
          Name = "Notification test activity",
          Description = "Test activity",
          StartDate = Start,
          EndDate = End
        });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ActivityDto>(TestContext.Current.CancellationToken);
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
          Name = "Notification test assignment",
          Description = "Test assignment",
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

  private async Task<List<NotificationDto>> GetNotificationsAsync(string token, bool unreadOnly = false)
  {
    var url = unreadOnly ? "/api/notifications?unreadOnly=true" : "/api/notifications";
    var response = await SendAuthorizedAsync(HttpMethod.Get, url, token);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    return (await response.Content.ReadFromJsonAsync<List<NotificationDto>>(TestContext.Current.CancellationToken))!;
  }

  private async Task<int> GetUnreadCountAsync(string token)
  {
    var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/notifications/unread-count", token);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<UnreadCountResponse>(TestContext.Current.CancellationToken);
    Assert.NotNull(body);
    return body.Count;
  }

  private record UnreadCountResponse(int Count);

  [Fact]
  public async Task TeacherAddsModuleResource_NotifiesEnrolledStudentsOnly()
  {
    var teacher = await CreateTeacherAsync("notify.module.teacher@example.com");
    var enrolled = await CreateStudentAsync("notify.module.enrolled@example.com");
    var outsider = await CreateStudentAsync("notify.module.outsider@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    await EnrollStudentAsync(enrolled.AccessToken, courseId);

    var resourceId = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Module slides",
      Url = "https://example.com/slides.pdf",
      ModuleId = moduleId
    });

    var enrolledNotifications = await GetNotificationsAsync(enrolled.AccessToken);
    var notification = Assert.Single(enrolledNotifications);
    Assert.Equal("ResourceAdded", notification.Type);
    Assert.Equal(moduleId, notification.ModuleId);
    Assert.Equal(resourceId, notification.ResourceId);
    Assert.Equal(courseId, notification.CourseId);
    Assert.False(notification.IsSeen);

    var outsiderNotifications = await GetNotificationsAsync(outsider.AccessToken);
    Assert.Empty(outsiderNotifications);
  }

  [Fact]
  public async Task TeacherAddsCourseResource_NotifiesStudents()
  {
    var teacher = await CreateTeacherAsync("notify.course.teacher@example.com");
    var student = await CreateStudentAsync("notify.course.student@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    await EnrollStudentAsync(student.AccessToken, courseId);

    await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Syllabus",
      Url = "https://example.com/syllabus.pdf",
      CourseId = courseId
    });

    var notifications = await GetNotificationsAsync(student.AccessToken);
    var notification = Assert.Single(notifications);
    Assert.Equal("ResourceAdded", notification.Type);
    Assert.Equal(courseId, notification.CourseId);
    Assert.Null(notification.ModuleId);
    Assert.Null(notification.ActivityId);
  }

  [Fact]
  public async Task TeacherAddsActivityResource_NotifiesStudents()
  {
    var teacher = await CreateTeacherAsync("notify.activity.teacher@example.com");
    var student = await CreateStudentAsync("notify.activity.student@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var activityId = await CreateActivityAsync(teacher.AccessToken, moduleId);
    await EnrollStudentAsync(student.AccessToken, courseId);

    await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Handout",
      Url = "https://example.com/handout.pdf",
      ActivityId = activityId
    });

    var notifications = await GetNotificationsAsync(student.AccessToken);
    var notification = Assert.Single(notifications);
    Assert.Equal("ResourceAdded", notification.Type);
    Assert.Equal(activityId, notification.ActivityId);
    Assert.Equal(courseId, notification.CourseId);
  }

  [Fact]
  public async Task StudentUpload_DoesNotNotifyClassmates()
  {
    var teacher = await CreateTeacherAsync("notify.upload.teacher@example.com");
    var uploader = await CreateStudentAsync("notify.upload.uploader@example.com");
    var classmate = await CreateStudentAsync("notify.upload.classmate@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    await EnrollStudentAsync(uploader.AccessToken, courseId);
    await EnrollStudentAsync(classmate.AccessToken, courseId);

    var response = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/resources",
        uploader.AccessToken,
        new CreateResourceRequestDto
        {
          DisplayName = "My draft",
          Url = "https://example.com/draft.pdf",
          ModuleId = moduleId
        });
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    Assert.Empty(await GetNotificationsAsync(classmate.AccessToken));
    Assert.Empty(await GetNotificationsAsync(uploader.AccessToken));
  }

  [Fact]
  public async Task GradeApproved_NotifiesStudent()
  {
    var teacher = await CreateTeacherAsync("notify.approve.teacher@example.com");
    var student = await CreateStudentAsync("notify.approve.student@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);
    await EnrollStudentAsync(student.AccessToken, courseId);

    var handIn = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/submissions",
        student.AccessToken,
        new CreateSubmissionRequestDto { AssignmentId = assignmentId, Content = "My essay" });
    Assert.Equal(HttpStatusCode.Created, handIn.StatusCode);
    var submission = (await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken))!;

    var grade = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/submissions/{submission.Id}",
        teacher.AccessToken,
        new UpdateSubmissionRequestDto { Status = "Approved" });
    grade.EnsureSuccessStatusCode();

    var notifications = await GetNotificationsAsync(student.AccessToken);
    var notification = Assert.Single(notifications);
    Assert.Equal("SubmissionApproved", notification.Type);
    Assert.Equal(submission.Id, notification.SubmissionId);
    Assert.Equal(courseId, notification.CourseId);
  }

  [Fact]
  public async Task GradeRevision_NotifiesStudent()
  {
    var teacher = await CreateTeacherAsync("notify.revision.teacher@example.com");
    var student = await CreateStudentAsync("notify.revision.student@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    var assignmentId = await CreateAssignmentAsync(teacher.AccessToken, moduleId);
    await EnrollStudentAsync(student.AccessToken, courseId);

    var handIn = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/submissions",
        student.AccessToken,
        new CreateSubmissionRequestDto { AssignmentId = assignmentId, Content = "My essay" });
    Assert.Equal(HttpStatusCode.Created, handIn.StatusCode);
    var submission = (await handIn.Content.ReadFromJsonAsync<SubmissionDto>(TestContext.Current.CancellationToken))!;

    var grade = await SendAuthorizedAsync(
        HttpMethod.Patch,
        $"/api/submissions/{submission.Id}",
        teacher.AccessToken,
        new UpdateSubmissionRequestDto { Status = "Revision" });
    grade.EnsureSuccessStatusCode();

    var notifications = await GetNotificationsAsync(student.AccessToken);
    var notification = Assert.Single(notifications);
    Assert.Equal("SubmissionReturned", notification.Type);
    Assert.Equal(submission.Id, notification.SubmissionId);
  }

  [Fact]
  public async Task GetNotifications_OrdersNewestFirst()
  {
    var teacher = await CreateTeacherAsync("notify.order.teacher@example.com");
    var student = await CreateStudentAsync("notify.order.student@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    await EnrollStudentAsync(student.AccessToken, courseId);

    var first = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "First",
      Url = "https://example.com/first.pdf",
      ModuleId = moduleId
    });
    var second = await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Second",
      Url = "https://example.com/second.pdf",
      ModuleId = moduleId
    });

    var notifications = await GetNotificationsAsync(student.AccessToken);
    Assert.Equal(2, notifications.Count);
    Assert.Equal(second, notifications[0].ResourceId);
    Assert.Equal(first, notifications[1].ResourceId);
  }

  [Fact]
  public async Task UnreadCount_ReflectsUnreadAndSeen()
  {
    var teacher = await CreateTeacherAsync("notify.count.teacher@example.com");
    var student = await CreateStudentAsync("notify.count.student@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    await EnrollStudentAsync(student.AccessToken, courseId);

    await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Slides",
      Url = "https://example.com/slides.pdf",
      ModuleId = moduleId
    });

    Assert.Equal(1, await GetUnreadCountAsync(student.AccessToken));

    var notifications = await GetNotificationsAsync(student.AccessToken);
    var notification = notifications[0];

    var markSeen = await SendAuthorizedAsync(
        HttpMethod.Post,
        $"/api/notifications/{notification.Id}/seen",
        student.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, markSeen.StatusCode);

    Assert.Equal(0, await GetUnreadCountAsync(student.AccessToken));
  }

  [Fact]
  public async Task MarkSeen_OnlyOwnerCanMarkOwnNotification()
  {
    var teacher = await CreateTeacherAsync("notify.seen.teacher@example.com");
    var owner = await CreateStudentAsync("notify.seen.owner@example.com");
    var other = await CreateStudentAsync("notify.seen.other@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    await EnrollStudentAsync(owner.AccessToken, courseId);

    await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Slides",
      Url = "https://example.com/slides.pdf",
      ModuleId = moduleId
    });

    var notification = (await GetNotificationsAsync(owner.AccessToken))[0];

    var otherAttempt = await SendAuthorizedAsync(
        HttpMethod.Post,
        $"/api/notifications/{notification.Id}/seen",
        other.AccessToken);
    Assert.Equal(HttpStatusCode.NotFound, otherAttempt.StatusCode);

    var ownerAttempt = await SendAuthorizedAsync(
        HttpMethod.Post,
        $"/api/notifications/{notification.Id}/seen",
        owner.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, ownerAttempt.StatusCode);

    var after = await GetNotificationsAsync(owner.AccessToken);
    Assert.True(after[0].IsSeen);
    Assert.NotNull(after[0].SeenAt);
  }

  [Fact]
  public async Task MarkAllSeen_SetsAllNotificationsSeen()
  {
    var teacher = await CreateTeacherAsync("notify.seenall.teacher@example.com");
    var student = await CreateStudentAsync("notify.seenall.student@example.com");

    var courseId = await CreateCourseAsync(teacher.AccessToken);
    var moduleId = await CreateModuleAsync(teacher.AccessToken, courseId);
    await EnrollStudentAsync(student.AccessToken, courseId);

    await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "First",
      Url = "https://example.com/first.pdf",
      ModuleId = moduleId
    });
    await CreateResourceAsync(teacher.AccessToken, new CreateResourceRequestDto
    {
      DisplayName = "Second",
      Url = "https://example.com/second.pdf",
      ModuleId = moduleId
    });

    var markAll = await SendAuthorizedAsync(
        HttpMethod.Post,
        "/api/notifications/seen-all",
        student.AccessToken);
    Assert.Equal(HttpStatusCode.NoContent, markAll.StatusCode);

    Assert.Equal(0, await GetUnreadCountAsync(student.AccessToken));

    var notifications = await GetNotificationsAsync(student.AccessToken);
    Assert.All(notifications, n => Assert.True(n.IsSeen));
  }
}

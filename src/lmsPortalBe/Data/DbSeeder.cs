using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Seeds default roles and an initial admin user on startup.

namespace lmsPortalBe.Data;

public static class DbSeeder
{
  private static readonly string[] DefaultRoles = { "student", "teacher", "admin" };

  public static async Task SeedAsync(this IHost host)
  {
    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var context = services.GetRequiredService<ILmsPortalContext>();
    var configuration = services.GetRequiredService<IConfiguration>();
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));

    foreach (var role in DefaultRoles)
    {
      if (await roleManager.RoleExistsAsync(role))
      {
        continue;
      }

      var result = await roleManager.CreateAsync(new IdentityRole(role));
      if (!result.Succeeded)
      {
        logger.LogWarning(
            "Role '{Role}' was not seeded: {Errors}",
            role,
            string.Join(", ", result.Errors.Select(e => e.Description)));
      }
    }

    await SeedAdminUserAsync(userManager, configuration, logger);
    await SeedDemoDataAsync(userManager, context, logger);
  }

  private static async Task SeedAdminUserAsync(
      UserManager<ApplicationUser> userManager,
      IConfiguration configuration,
      ILogger logger)
  {
    var username = configuration["ADMIN_USERNAME"];
    var email = configuration["ADMIN_EMAIL"];
    var password = configuration["ADMIN_PASSWORD"];
    var firstName = configuration["ADMIN_FIRST_NAME"];
    var lastName = configuration["ADMIN_LAST_NAME"];

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(password))
    {
      logger.LogInformation("Admin credentials not configured; skipping admin user seeding.");
      return;
    }

    var admin = await userManager.FindByEmailAsync(email)
        ?? await userManager.FindByNameAsync(username);
    if (admin is null)
    {
      admin = new ApplicationUser
      {
        UserName = username,
        Email = email,
        EmailConfirmed = true,
        FirstName = firstName ?? string.Empty,
        LastName = lastName ?? string.Empty
      };

      var createResult = await userManager.CreateAsync(admin, password);
      if (!createResult.Succeeded)
      {
        logger.LogWarning(
            "Admin user '{Email}' was not seeded: {Errors}",
            email,
            string.Join(", ", createResult.Errors.Select(e => e.Description)));
        return;
      }
    }

    if (!await userManager.IsInRoleAsync(admin, "admin"))
    {
      var roleResult = await userManager.AddToRoleAsync(admin, "admin");
      if (!roleResult.Succeeded)
      {
        logger.LogWarning(
            "Could not assign admin role to '{Email}': {Errors}",
            email,
            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
      }
    }
  }

  private static async Task SeedDemoDataAsync(
      UserManager<ApplicationUser> userManager,
      ILmsPortalContext context,
      ILogger logger)
  {
    const string demoPassword = "Passw0rd1";

    if (await context.Courses.AnyAsync())
    {
      logger.LogInformation("Demo data already present; skipping demo seeding.");
      return;
    }

    var seededAssignments = new List<(Assignment Assignment, CourseModel Course)>();
    var seededStudents = new List<(ApplicationUser Student, CourseModel Course)>();
    var seededModules = new List<(CourseModule Module, CourseModel Course)>();
    var seededActivities = new List<(Activity Activity, CourseModel Course)>();
    var seededTeachers = new List<(ApplicationUser Teacher, CourseModel Course)>();

    // COURSES
    var mathCourse = new CourseModel
    {
      Name = "Mathematics 101",
      Description = "Introduction to algebra and geometry.",
      StartDate = new DateTime(2026, 9, 14, 9, 0, 0),
      EndDate = new DateTime(2026, 12, 18, 17, 0, 0)
    };

    var historyCourse = new CourseModel
    {
      Name = "History 101",
      Description = "A survey of world history.",
      StartDate = new DateTime(2026, 9, 14, 9, 0, 0),
      EndDate = new DateTime(2026, 12, 18, 17, 0, 0)
    };

    var csCourse = new CourseModel
    {
      Name = "Computer Science 101",
      Description = "Introduction to programming and computation.",
      StartDate = new DateTime(2026, 8, 31, 9, 0, 0),
      EndDate = new DateTime(2026, 12, 11, 17, 0, 0)
    };

    var backendCourse = new CourseModel
    {
      Name = "Backend Development",
      Description = "Build APIs and services with ASP.NET Core.",
      StartDate = DateTime.Today,
      EndDate = DateTime.Today.AddMonths(3)
    };

    context.Courses.Add(mathCourse);
    context.Courses.Add(historyCourse);
    context.Courses.Add(csCourse);
    context.Courses.Add(backendCourse);
    await context.SaveChangesAsync();

    // MODULES
    var modules = new (string Name, string Description, DateTime StartDate, DateTime EndDate, CourseModel Course)[]
    {
      ("Algebra 101", "Learn the basics of algebra", new DateTime(2026, 9, 14, 9, 0, 0), new DateTime(2026, 10, 18, 17, 0, 0), mathCourse),
      ("Geometry 101", "Learn the basics of geometry", new DateTime(2026, 10, 19, 17, 0, 0), new DateTime(2026, 12, 18, 17, 0, 0), mathCourse),
      ("Ancient egypt", "They had pyramids", new DateTime(2026, 9, 14, 9, 0, 0), new DateTime(2026, 10, 18, 17, 0, 0), historyCourse),
      ("Ancient maya", "Also had pyramids", new DateTime(2026, 10, 19, 17, 0, 0), new DateTime(2026, 12, 18, 17, 0, 0), historyCourse),
      ("Programming Basics", "Write your first program", new DateTime(2026, 8, 31, 9, 0, 0), new DateTime(2026, 9, 30, 17, 0, 0), csCourse),
      ("Data Structures", "Lists, stacks, and queues", new DateTime(2026, 10, 1, 9, 0, 0), new DateTime(2026, 10, 31, 17, 0, 0), csCourse),
    };

    foreach (var (name, description, start, end, course) in modules)
    {
      var module = new CourseModule { Name = name, Description = description, StartDate = start, EndDate = end, Course = course, CourseId = course.Id };

      // ACTIVITIES
      var firstActivity = new Activity
      {
        Name = $"{name} — Lecture",
        Description = $"Introductory lecture covering the core topics of {name}.",
        StartDate = start,
        EndDate = start.AddHours(2),
        ActivityType = ActivityType.Lecture
      };
      var secondActivity = new Activity
      {
        Name = $"{name} — Mentorship Session",
        Description = $"Guided mentorship session for {name}.",
        StartDate = start.AddDays(1),
        EndDate = start.AddDays(1).AddHours(2),
        ActivityType = ActivityType.Mentorship
      };
      module.Activities.Add(firstActivity);
      module.Activities.Add(secondActivity);
      seededActivities.Add((firstActivity, course));
      seededActivities.Add((secondActivity, course));

      // ASSIGNMENTS
      var firstAssignment = new Assignment
      {
        Name = $"{name} — Introduction",
        Description = $"Introduction to the core concepts of {name}.",
        DueDate = start
      };
      var secondAssignment = new Assignment
      {
        Name = $"{name} — Applied Practice",
        Description = $"Apply the concepts from {name} to a hands-on task.",
        DueDate = start.AddDays(1)
      };
      module.Assignments.Add(firstAssignment);
      module.Assignments.Add(secondAssignment);
      seededAssignments.Add((firstAssignment, course));
      seededAssignments.Add((secondAssignment, course));
      seededModules.Add((module, course));

      context.CourseModules.Add(module);
    }

    await context.SaveChangesAsync();

    // BACKEND COURSE MODULES
    var backendModules = new (string Name, string Description, DateTime StartDate, DateTime EndDate)[]
    {
      ("C# Fundamentals", "Syntax, types, and OOP basics.", backendCourse.StartDate, backendCourse.StartDate.AddMonths(1)),
      ("ASP.NET Core", "Controllers, routing, and middleware.", backendCourse.StartDate.AddMonths(1), backendCourse.StartDate.AddMonths(2)),
      ("Databases & EF Core", "Relational data access and migrations.", backendCourse.StartDate.AddMonths(2), backendCourse.EndDate),
    };

    foreach (var (name, description, start, end) in backendModules)
    {
      var module = new CourseModule
      {
        Name = name,
        Description = description,
        StartDate = start,
        EndDate = end,
        Course = backendCourse,
        CourseId = backendCourse.Id
      };

      var firstAssignment = new Assignment
      {
        Name = $"{name} — Introduction",
        Description = $"Introduction to the core concepts of {name}.",
        DueDate = start
      };
      var secondAssignment = new Assignment
      {
        Name = $"{name} — Applied Practice",
        Description = $"Apply the concepts from {name} to a hands-on task.",
        DueDate = start.AddDays(1)
      };
      module.Assignments.Add(firstAssignment);
      module.Assignments.Add(secondAssignment);
      seededAssignments.Add((firstAssignment, backendCourse));
      seededAssignments.Add((secondAssignment, backendCourse));
      seededModules.Add((module, backendCourse));

      context.CourseModules.Add(module);
    }

    await context.SaveChangesAsync();



    // TEACHERS
    var teachers = new (string FirstName, string LastName, CourseModel Course)[]
    {
      ("Alan", "Turing", mathCourse),
      ("Marie", "Curie", historyCourse),
      ("Ada", "Lovelace", csCourse)
    };

    foreach (var (firstName, lastName, course) in teachers)
    {
      var email = $"{firstName}.{lastName}@example.com".ToLowerInvariant();
      var teacher = await CreateDemoUserAsync(
          userManager, firstName, lastName, email, demoPassword, "teacher", logger);
      if (teacher is null)
      {
        continue;
      }

      await EnsureProfileAsync(context, teacher, firstName, lastName, course.Name, "teacher");

      context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = course.Id,
        UserId = teacher.Id,
        Role = CourseRole.Teacher
      });

      context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = backendCourse.Id,
        UserId = teacher.Id,
        Role = CourseRole.Teacher
      });

      seededTeachers.Add((teacher, course));
    }

    // STUDENTS
    var students = new (string FirstName, string LastName, CourseModel Course)[]
    {
      ("Alice", "Johnson", mathCourse),
      ("Bob", "Smith", mathCourse),
      ("Carol", "Davis", mathCourse),
      ("David", "Wilson", mathCourse),
      ("Eve", "Brown", mathCourse),
      ("Frank", "Miller", historyCourse),
      ("Grace", "Lee", historyCourse),
      ("Henry", "Moore", historyCourse),
      ("Ivy", "Taylor", historyCourse),
      ("Jack", "Anderson", historyCourse),
      ("Liam", "Carter", csCourse),
      ("Noah", "Brooks", csCourse),
      ("Olivia", "Foster", csCourse),
      ("Emma", "Reed", csCourse),
      ("Sophia", "Hayes", csCourse),
      ("Mia", "Bennett", csCourse),
      ("Lucas", "Grant", csCourse)
    };

    foreach (var (firstName, lastName, course) in students)
    {
      var email = $"{firstName}.{lastName}@example.com".ToLowerInvariant();
      var student = await CreateDemoUserAsync(
          userManager, firstName, lastName, email, demoPassword, "student", logger);
      if (student is null)
      {
        continue;
      }

      await EnsureProfileAsync(context, student, firstName, lastName, course.Name, "student");

      context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = course.Id,
        UserId = student.Id,
        Role = CourseRole.Student
      });

      context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = backendCourse.Id,
        UserId = student.Id,
        Role = CourseRole.Student
      });

      seededStudents.Add((student, course));
    }

    // SUBMISSIONS — deterministic demo mix so the teacher "pending" list has data.
    var random = new Random(20260910);

    foreach (var (student, course) in seededStudents)
    {
      var courseAssignments = seededAssignments
          .Where(a => a.Course.Id == course.Id)
          .OrderBy(a => a.Assignment.DueDate)
          .ToList();

      foreach (var (assignment, _) in courseAssignments.Take(2))
      {
        var roll = random.Next(0, 4);
        var (status, feedback) = roll switch
        {
          0 => (AssignmentStatus.Approved, "Great work, well done!"),
          1 => (AssignmentStatus.Revision, "Please revise section two and resubmit."),
          _ => (AssignmentStatus.HandedIn, string.Empty)
        };

        context.Submissions.Add(new Submission
        {
          AssignmentId = assignment.Id,
          StudentId = student.Id,
          Content = $"Demo submission by {student.FirstName} {student.LastName} for '{assignment.Name}'.",
          Feedback = feedback,
          Status = status,
          HandinDate = DateTime.UtcNow.AddDays(-random.Next(1, 8))
        });
      }
    }

    // RESUBMISSIONS — give the first student in each course a full
    // hand-in → rejected → resubmitted history so the frontend has data to
    // exercise the per-assignment/student history and new deadlines.
    foreach (var course in new[] { mathCourse, historyCourse, csCourse })
    {
      var student = seededStudents.First(s => s.Course.Id == course.Id).Student;
      var assignment = seededAssignments
          .Where(a => a.Course.Id == course.Id)
          .OrderBy(a => a.Assignment.DueDate)
          .Select(a => a.Assignment)
          .First();

      var original = context.Submissions.Local
          .FirstOrDefault(s => s.AssignmentId == assignment.Id && s.StudentId == student.Id);
      if (original is null)
      {
        continue;
      }

      original.HandinDate = DateTime.UtcNow.AddDays(-5);
      original.Status = AssignmentStatus.Revision;
      original.Feedback = "Please revise section two and resubmit.";
      original.GradedAt = DateTime.UtcNow.AddDays(-3);

      context.Submissions.Add(new Submission
      {
        AssignmentId = assignment.Id,
        StudentId = student.Id,
        Content = $"Resubmission by {student.FirstName} {student.LastName} for '{assignment.Name}'.",
        Status = AssignmentStatus.HandedIn,
        HandinDate = DateTime.UtcNow.AddDays(-1)
      });
    }

    await context.SaveChangesAsync();

    // RESOURCES — teacher course material and student uploads.
    var teacherByCourse = seededTeachers.ToDictionary(t => t.Course, t => t.Teacher);
    var backendTeacher = teacherByCourse[csCourse];

    string CreatorIdFor(CourseModel course) =>
        course == backendCourse ? backendTeacher.Id : teacherByCourse[course].Id;

    var now = DateTime.UtcNow;

    // Course-level resources.
    var courseResources = new (CourseModel Course, string Name, string Url)[]
    {
      (mathCourse, "Course Syllabus", "https://example.com/math/syllabus.pdf"),
      (mathCourse, "Recommended Reading List", "https://example.com/math/reading-list.pdf"),
      (historyCourse, "Course Syllabus", "https://example.com/history/syllabus.pdf"),
      (historyCourse, "World History Timeline", "https://example.com/history/timeline.pdf"),
      (csCourse, "Course Syllabus", "https://example.com/cs/syllabus.pdf"),
      (csCourse, "Development Environment Setup", "https://example.com/cs/setup.pdf"),
      (backendCourse, "API Design Guide", "https://example.com/backend/api-design.pdf"),
      (backendCourse, "Backend Roadmap", "https://example.com/backend/roadmap.pdf"),
    };

    foreach (var (course, name, url) in courseResources)
    {
      context.Resources.Add(new Resource
      {
        DisplayName = name,
        Url = url,
        CourseId = course.Id,
        CreatorId = CreatorIdFor(course),
        UploadDate = now.AddDays(-30),
        LastEditDate = now.AddDays(-5)
      });
    }

    // Module-level resources.
    foreach (var (module, course) in seededModules)
    {
      context.Resources.Add(new Resource
      {
        DisplayName = $"{module.Name} — Lecture Slides",
        Url = $"https://example.com/slides/{module.Id}.pdf",
        ModuleId = module.Id,
        CreatorId = CreatorIdFor(course),
        UploadDate = now.AddDays(-20),
        LastEditDate = now.AddDays(-3)
      });
    }

    // Activity-level resources.
    foreach (var (activity, course) in seededActivities)
    {
      context.Resources.Add(new Resource
      {
        DisplayName = $"{activity.Name} — Handout",
        Url = $"https://example.com/handouts/{activity.Id}.pdf",
        ActivityId = activity.Id,
        CreatorId = CreatorIdFor(course),
        UploadDate = now.AddDays(-10),
        LastEditDate = now.AddDays(-1)
      });
    }

    // Student uploads (module-attached, flagged as student submissions).
    foreach (var (student, course) in seededStudents)
    {
      var module = seededModules.First(m => m.Course.Id == course.Id).Module;

      context.Resources.Add(new Resource
      {
        DisplayName = $"{student.FirstName}'s project draft",
        Url = $"https://example.com/uploads/{student.Id}.pdf",
        ModuleId = module.Id,
        CreatorId = student.Id,
        IsStudentSubmitted = true,
        UploadDate = now.AddDays(-random.Next(1, 10)),
        LastEditDate = now.AddDays(-random.Next(1, 5))
      });
    }

    await context.SaveChangesAsync();

    logger.LogInformation(
        "Seeded demo courses: '{Course1}', '{Course2}', '{Course3}' and '{Course4}', with 3 teachers and 17 students.",
        mathCourse.Name,
        historyCourse.Name,
        csCourse.Name,
        backendCourse.Name);
  }

  private static async Task<ApplicationUser?> CreateDemoUserAsync(
      UserManager<ApplicationUser> userManager,
      string firstName,
      string lastName,
      string email,
      string password,
      string role,
      ILogger logger)
  {
    var existing = await userManager.FindByEmailAsync(email);
    if (existing is not null)
    {
      return existing;
    }

    var user = new ApplicationUser
    {
      UserName = email,
      Email = email,
      EmailConfirmed = true,
      FirstName = firstName,
      LastName = lastName
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
      logger.LogWarning(
          "Could not seed demo user '{Email}': {Errors}",
          email,
          string.Join(", ", createResult.Errors.Select(e => e.Description)));
      return null;
    }

    var roleResult = await userManager.AddToRoleAsync(user, role);
    if (!roleResult.Succeeded)
    {
      logger.LogWarning(
          "Could not assign role '{Role}' to demo user '{Email}': {Errors}",
          role,
          email,
          string.Join(", ", roleResult.Errors.Select(e => e.Description)));
    }

    return user;
  }

  private static async Task EnsureProfileAsync(
      ILmsPortalContext context,
      ApplicationUser user,
      string firstName,
      string lastName,
      string courseName,
      string role)
  {
    if (await context.UserProfiles.AnyAsync(p => p.UserId == user.Id))
    {
      return;
    }

    var skills = role == "teacher"
        ? new List<string> { courseName, "Mentoring" }
        : new List<string> { courseName, "Teamwork" };

    var daysOffset = (firstName.Length * 137 + lastName.Length * 97) % 4000;

    context.UserProfiles.Add(new UserProfile
    {
      UserId = user.Id,
      AboutMe = $"{firstName} {lastName} — demo {role} account.",
      GitHubLink = $"https://github.com/{firstName.ToLowerInvariant()}{lastName.ToLowerInvariant()}",
      Skills = skills,
      DateOfBirth = new DateOnly(1985, 1, 1).AddDays(daysOffset)
    });
  }
}

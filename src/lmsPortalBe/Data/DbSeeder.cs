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

    context.Courses.Add(mathCourse);
    context.Courses.Add(historyCourse);
    context.Courses.Add(csCourse);
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
      var firstActivity = new Activity { Name = "First Activity", Description = "First", StartDate = start, EndDate = start.AddHours(2), ActivityType = ActivityType.Lecture };
      var secondActivity = new Activity { Name = "Second Activity", Description = "Second", StartDate = start.AddDays(1), EndDate = start.AddDays(1).AddHours(2), ActivityType = ActivityType.Mentorship };
      module.Activities.Add(firstActivity);
      module.Activities.Add(secondActivity);
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

      context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = course.Id,
        UserId = teacher.Id,
        Role = CourseRole.Teacher
      });
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

      context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = course.Id,
        UserId = student.Id,
        Role = CourseRole.Student
      });
    }

    await context.SaveChangesAsync();

    logger.LogInformation(
        "Seeded demo courses: '{Course1}', '{Course2}' and '{Course3}', with 3 teachers and 17 students.",
        mathCourse.Name,
        historyCourse.Name,
        csCourse.Name);
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
}

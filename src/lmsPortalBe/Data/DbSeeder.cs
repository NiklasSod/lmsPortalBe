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

  // The single point in time the whole demo world is anchored to.
  // Change this one date to move every seeded date into the past or future.
  private static readonly DateTime Anchor = new(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);

  private static DateTime D(int days, int hour = 9, int minute = 0)
  {
    var date = Anchor.AddDays(days);
    return new DateTime(date.Year, date.Month, date.Day, hour, minute, 0, DateTimeKind.Utc);
  }

  private static int DaysFromAnchor(DateTime value) => (int)(value.Date - Anchor.Date).TotalDays;

  private static CourseModel AddCourse(
      ILmsPortalContext context,
      string name,
      string description,
      int startDay,
      int endDay,
      params SeedModule[] seedModules)
  {
    var course = new CourseModel
    {
      Name = name,
      Description = description,
      StartDate = D(startDay),
      EndDate = D(endDay, 17)
    };

    foreach (var seed in seedModules)
    {
      var module = new CourseModule
      {
        Name = seed.Name,
        Description = seed.Description,
        StartDate = D(seed.StartDay),
        EndDate = D(seed.EndDay, 17),
        Course = course
      };

      foreach (var seedActivity in seed.Activities)
      {
        var start = D(seedActivity.Day, seedActivity.Hour, seedActivity.Minute);
        module.Activities.Add(new Activity
        {
          Name = seedActivity.Name,
          Description = seedActivity.Description,
          ActivityType = seedActivity.Type,
          Module = module,
          StartDate = start,
          EndDate = start.AddHours(seedActivity.DurationHours)
        });
      }

      foreach (var seedAssignment in seed.Assignments)
      {
        module.Assignments.Add(new Assignment
        {
          Name = seedAssignment.Name,
          Description = seedAssignment.Description,
          Module = module,
          DueDate = D(seedAssignment.DueDay, 23, 59)
        });
      }

      course.Modules.Add(module);
    }

    context.Courses.Add(course);
    return course;
  }

  private sealed record SeedModule(
      string Name,
      string Description,
      int StartDay,
      int EndDay,
      SeedActivity[] Activities,
      SeedAssignment[] Assignments);

  private sealed record SeedActivity(
      string Name,
      string Description,
      ActivityType Type,
      int Day,
      int Hour,
      int DurationHours,
      int Minute = 0);

  private sealed record SeedAssignment(
      string Name,
      string Description,
      int DueDay);

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
    var environment = services.GetRequiredService<IHostEnvironment>();
    if (environment.IsDevelopment())
    {
      await SeedDemoDataAsync(userManager, context, logger);
    }
    else
    {
      logger.LogInformation(
          "Skipping demo data seeding (environment: {Environment}).",
          environment.EnvironmentName);
    }
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

    // Every seeded date is derived from Anchor, so moving Anchor shifts the
    // whole demo timeline into the past or the future.
    var math = AddCourse(context,
        "Mathematics I — Calculus & Linear Algebra",
        "A first university course in calculus and linear algebra. It develops a rigorous understanding of functions, limits, derivatives and vector spaces, with weekly problem-solving sessions that connect the theory to applications in physics, economics and engineering.",
        -49, 63,
        new SeedModule("Foundations: Sets, Functions & Limits",
            "The building blocks of mathematical reasoning. We formalise the notion of a function, study limits and continuity, and practise writing clear, step-by-step mathematical arguments.",
            -49, -21,
            [
              new SeedActivity("Course Introduction & Sets", "Welcome lecture covering course structure, assessment and the language of sets and logic.", ActivityType.Lecture, -49, 9, 2),
              new SeedActivity("Functions & Graphs Workshop", "Hands-on workshop exploring domains, ranges and transformations of elementary functions.", ActivityType.Workshop, -46, 13, 2),
              new SeedActivity("Limits Practice Session", "Guided problem solving on limits, continuity and asymptotic behaviour.", ActivityType.Practice, -42, 9, 2)
            ],
            [
              new SeedAssignment("Problem Set 1 — Sets and Functions", "Written problem set on set notation, function composition and inverse functions.", -38),
              new SeedAssignment("Problem Set 2 — Limits and Continuity", "Exercises on evaluating limits, continuity and the intermediate value theorem.", -21)
            ]),
        new SeedModule("Differentiation",
            "From the limit definition of the derivative to the chain rule, implicit differentiation and applications such as optimisation and related rates.",
            -20, 14,
            [
              new SeedActivity("The Derivative & Differentiation Rules", "Lecture introducing the derivative and the core differentiation rules.", ActivityType.Lecture, -20, 9, 2),
              new SeedActivity("Applications of Differentiation", "Seminar on optimisation, curve sketching and related rates.", ActivityType.Seminar, -13, 10, 2),
              new SeedActivity("Exam Preparation Mentorship", "Small-group mentorship session reviewing the material ahead of the midterm exam.", ActivityType.Mentorship, -6, 15, 1)
            ],
            [
              new SeedAssignment("Problem Set 3 — Differentiation Techniques", "Exercises covering the product, quotient and chain rules and implicit differentiation.", -7),
              new SeedAssignment("Midterm Exam — Calculus I", "Timed exam covering limits, continuity and differentiation.", 7)
            ]),
        new SeedModule("Linear Algebra & Vectors",
            "Vectors, matrices, systems of linear equations and eigenvalues, ending with a final project that applies linear algebra to real-world data.",
            15, 63,
            [
              new SeedActivity("Vectors & Matrices", "Lecture introducing vectors, dot products and matrix operations.", ActivityType.Lecture, 15, 9, 2),
              new SeedActivity("Solving Linear Systems", "Workshop on Gaussian elimination and matrix inverses.", ActivityType.Workshop, 22, 13, 2),
              new SeedActivity("Eigenvalues Practice", "Guided practice on eigenvectors, eigenvalues and diagonalisation.", ActivityType.Practice, 29, 9, 2)
            ],
            [
              new SeedAssignment("Problem Set 4 — Linear Systems & Matrices", "Exercises on solving linear systems and performing matrix operations.", 30),
              new SeedAssignment("Final Project — Applied Linear Algebra", "Apply linear algebra techniques to a dataset of your choice and report your findings.", 56)
            ]));

    var history = AddCourse(context,
        "World History: Antiquity to the Modern Age",
        "A thematic survey of world history from the earliest civilisations to the early modern era. Emphasis is placed on long-term causes and consequences, primary-source analysis and the connectedness of societies through trade, religion and technology.",
        -45, 59,
        new SeedModule("The Ancient World",
            "The first cities and empires of Mesopotamia, Egypt, the Indus Valley and the Mediterranean, and the institutions they left behind.",
            -45, -22,
            [
              new SeedActivity("First Civilisations", "Lecture on the rise of the first cities and empires in the ancient world.", ActivityType.Lecture, -45, 10, 2),
              new SeedActivity("Primary Sources from Antiquity", "Seminar discussing and analysing written sources from the ancient world.", ActivityType.Seminar, -38, 13, 2),
              new SeedActivity("Egypt & Mesopotamia Timeline", "Self-paced e-learning module on the chronology of the great river civilisations.", ActivityType.ELearning, -31, 9, 1)
            ],
            [
              new SeedAssignment("Source Analysis — The Code of Hammurabi", "Analyse the Code of Hammurabi as a primary source and place it in its historical context.", -24)
            ]),
        new SeedModule("The Middle Ages",
            "Feudalism, the spread of Islam, the Crusades and the Black Death, with a focus on trade networks and the movement of ideas.",
            -21, 8,
            [
              new SeedActivity("Feudalism & the Church", "Lecture on the political and religious structures of medieval Europe.", ActivityType.Lecture, -21, 10, 2),
              new SeedActivity("Mapping the Silk Road", "Workshop tracing trade routes and cultural exchange across Eurasia.", ActivityType.Workshop, -14, 13, 2),
              new SeedActivity("Essay Planning Mentorship", "Mentorship session on structuring and planning a historical essay.", ActivityType.Mentorship, -7, 15, 1)
            ],
            [
              new SeedAssignment("Essay — The Impact of the Crusades", "Write a short essay evaluating the long-term consequences of the Crusades.", 0)
            ]),
        new SeedModule("The Early Modern Era",
            "The Renaissance, the age of exploration, the Reformation and the beginnings of global trade and colonialism.",
            9, 59,
            [
              new SeedActivity("Renaissance & Reformation", "Lecture on the intellectual and religious transformations of early modern Europe.", ActivityType.Lecture, 9, 10, 2),
              new SeedActivity("Exploration and Its Consequences", "Seminar on the age of exploration and its global consequences.", ActivityType.Seminar, 16, 13, 2),
              new SeedActivity("Exam Question Practice", "Guided practice answering exam-style questions on the early modern era.", ActivityType.Practice, 23, 9, 2)
            ],
            [
              new SeedAssignment("Research Paper — A Turning Point in World History", "Choose a historical turning point and write a research paper supported by primary and secondary sources.", 40)
            ]));

    var cs = AddCourse(context,
        "Introduction to Computer Science",
        "An introduction to programming and computational thinking. Students learn to write, test and debug programs in a modern language, then build data structures and study algorithms while practising good software-engineering habits from day one.",
        -52, 67,
        new SeedModule("Programming Fundamentals",
            "Variables, control flow, functions and basic debugging. By the end of this module you can write small, well-structured programs from scratch.",
            -52, -28,
            [
              new SeedActivity("Hello, World & Variables", "First lecture: values, variables, types and your first program.", ActivityType.Lecture, -52, 9, 2),
              new SeedActivity("Control Flow & Functions Lab", "Lab session on conditionals, loops and writing reusable functions.", ActivityType.Workshop, -45, 13, 3),
              new SeedActivity("Debugging Practice", "Guided practice on reading error messages and debugging small programs.", ActivityType.Practice, -38, 9, 2)
            ],
            [
              new SeedAssignment("Lab 1 — First Programs", "Write a set of small programs practising variables, conditionals and loops.", -33)
            ]),
        new SeedModule("Data Structures & Algorithms",
            "Arrays, linked lists, stacks, queues and dictionaries, together with the fundamentals of algorithmic complexity and sorting.",
            -27, 0,
            [
              new SeedActivity("Lists, Stacks & Queues", "Lecture introducing the classic linear data structures.", ActivityType.Lecture, -27, 9, 2),
              new SeedActivity("Implementing Data Structures", "Workshop implementing data structures from scratch and testing them.", ActivityType.Workshop, -20, 13, 3),
              new SeedActivity("Big-O Notation in Practice", "Seminar on analysing and comparing the complexity of algorithms.", ActivityType.Seminar, -13, 10, 2)
            ],
            [
              new SeedAssignment("Lab 2 — Data Structures", "Implement and test a set of core data structures.", -7),
              new SeedAssignment("Quiz — Algorithmic Complexity", "Online quiz on Big-O notation and algorithmic complexity.", 0)
            ]),
        new SeedModule("Object-Oriented Design",
            "Classes, objects, inheritance and interfaces, with an emphasis on designing maintainable, testable code.",
            1, 30,
            [
              new SeedActivity("Classes & Objects", "Lecture on encapsulation, constructors and object state.", ActivityType.Lecture, 1, 9, 2),
              new SeedActivity("Modelling a Domain in Code", "Workshop modelling a real-world domain using classes and relationships.", ActivityType.Workshop, 8, 13, 3),
              new SeedActivity("Inheritance & Interfaces", "Guided practice on inheritance, interfaces and polymorphism.", ActivityType.Practice, 15, 9, 2)
            ],
            [
              new SeedAssignment("Project — Design a Class Hierarchy", "Design and implement a class hierarchy for a small domain of your choice.", 14)
            ]),
        new SeedModule("Software Engineering Practices",
            "Version control, testing, debugging and collaboration, culminating in a small group project.",
            31, 67,
            [
              new SeedActivity("Git & Version Control", "Lecture on branching, merging and collaborative workflows.", ActivityType.Lecture, 31, 9, 2),
              new SeedActivity("Writing Unit Tests", "Workshop on writing and running automated unit tests.", ActivityType.Workshop, 38, 13, 3),
              new SeedActivity("Group Project Mentorship", "Mentorship session supporting the final group project.", ActivityType.Mentorship, 45, 15, 1)
            ],
            [
              new SeedAssignment("Final Project — Build a Small Application", "Work in a group to design, build and test a small application end to end.", 60)
            ]));

    var backend = AddCourse(context,
        "Backend Development with ASP.NET Core",
        "A hands-on course in building production-ready web APIs with C# and ASP.NET Core. Covering routing, controllers, authentication, persistence with EF Core and deployment, the course ends with a complete REST API built from scratch.",
        -35, 70,
        new SeedModule("C# Fundamentals",
            "Syntax, types, collections, LINQ and object-oriented programming in C#, aimed at developers moving from other languages.",
            -35, -14,
            [
              new SeedActivity("C# Types & Collections", "Lecture on the C# type system, generics and collections.", ActivityType.Lecture, -35, 9, 2),
              new SeedActivity("LINQ & Lambda Expressions", "Workshop on querying collections with LINQ and lambda expressions.", ActivityType.Workshop, -28, 13, 3),
              new SeedActivity("OOP in C#", "Guided practice on classes, inheritance and interfaces in C#.", ActivityType.Practice, -21, 9, 2)
            ],
            [
              new SeedAssignment("Exercise — Console Application", "Build a small console application that demonstrates C# fundamentals.", -15)
            ]),
        new SeedModule("Building Web APIs with ASP.NET Core",
            "Controllers, routing, model binding, middleware, dependency injection and RESTful API design.",
            -13, 14,
            [
              new SeedActivity("Controllers & Routing", "Lecture on controllers, action methods and route templates.", ActivityType.Lecture, -13, 9, 2),
              new SeedActivity("Building Your First API", "Workshop building a complete CRUD API from scratch.", ActivityType.Workshop, -6, 13, 3),
              new SeedActivity("REST Design Patterns", "Seminar on REST conventions, status codes and versioning.", ActivityType.Seminar, 1, 10, 2)
            ],
            [
              new SeedAssignment("Assignment — Bookstore API", "Implement a RESTful API for a bookstore with full CRUD endpoints.", 7)
            ]),
        new SeedModule("Databases & EF Core",
            "Relational modelling, migrations, querying with LINQ and transaction handling using Entity Framework Core.",
            15, 42,
            [
              new SeedActivity("Relational Models & Migrations", "Lecture on modelling relationships and applying EF Core migrations.", ActivityType.Lecture, 15, 9, 2),
              new SeedActivity("EF Core in Practice", "Workshop writing LINQ queries and managing transactions.", ActivityType.Workshop, 22, 13, 3),
              new SeedActivity("Query Optimisation", "Guided practice on indexing and writing efficient queries.", ActivityType.Practice, 29, 9, 2)
            ],
            [
              new SeedAssignment("Assignment — Data Layer for the Bookstore API", "Add a persistence layer using EF Core with migrations and seeding.", 28)
            ]),
        new SeedModule("Authentication, Testing & Deployment",
            "JWT authentication, authorisation policies, integration testing and preparing an API for production.",
            43, 70,
            [
              new SeedActivity("JWT & Authorisation", "Lecture on token-based authentication and role-based authorisation.", ActivityType.Lecture, 43, 9, 2),
              new SeedActivity("Integration Testing", "Workshop writing integration tests for an ASP.NET Core API.", ActivityType.Workshop, 50, 13, 3),
              new SeedActivity("Deployment Mentorship", "Mentorship session on publishing and deploying an API to production.", ActivityType.Mentorship, 57, 15, 1)
            ],
            [
              new SeedAssignment("Final Project — Complete the LMS API", "Finish the course project by adding authentication, tests and deployment notes.", 63)
            ]));

    var courses = new[] { math, history, cs, backend };
    await context.SaveChangesAsync();

    var modules = courses
        .SelectMany(c => c.Modules.Select(m => (Module: m, Course: c)))
        .ToList();
    var activities = modules
        .SelectMany(x => x.Module.Activities.Select(a => (Activity: a, Course: x.Course)))
        .ToList();
    var assignments = modules
        .SelectMany(x => x.Module.Assignments.Select(a => (Assignment: a, Course: x.Course)))
        .ToList();

    // TEACHERS — one per course.
    var teachers = new (string FirstName, string LastName, CourseModel Course)[]
    {
      ("Anna", "Lindqvist", math),
      ("Johan", "Berg", history),
      ("Maria", "Eklund", cs),
      ("Erik", "Holm", backend)
    };

    var teacherByCourse = new Dictionary<CourseModel, ApplicationUser>();

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
        Role = CourseRole.Teacher,
        EnrolledAt = course.StartDate.AddDays(-1)
      });

      teacherByCourse[course] = teacher;
    }

    // STUDENTS — each takes a primary course plus one more, so everyone has
    // more than one course on their schedule.
    var students = new (string FirstName, string LastName, CourseModel Course)[]
    {
      ("Lucas", "Andersson", math),
      ("Emma", "Johansson", math),
      ("Oscar", "Karlsson", math),
      ("Elsa", "Nilsson", math),
      ("Hugo", "Eriksson", math),
      ("Alice", "Larsson", history),
      ("William", "Olsson", history),
      ("Astrid", "Persson", history),
      ("Noah", "Svensson", history),
      ("Maja", "Gustafsson", history),
      ("Liam", "Pettersson", cs),
      ("Ella", "Jonsson", cs),
      ("Axel", "Hagglund", cs),
      ("Wilma", "Sandberg", cs),
      ("Elias", "Holmqvist", cs),
      ("Olivia", "Wikstrom", cs),
      ("Nils", "Sjoberg", backend),
      ("Saga", "Lindgren", backend),
      ("Leo", "Nystrom", backend),
      ("Freja", "Dahlberg", backend)
    };

    var seededStudents = new List<(ApplicationUser Student, CourseModel Course)>();

    foreach (var (firstName, lastName, primary) in students)
    {
      var email = $"{firstName}.{lastName}@example.com".ToLowerInvariant();
      var student = await CreateDemoUserAsync(
          userManager, firstName, lastName, email, demoPassword, "student", logger);
      if (student is null)
      {
        continue;
      }

      await EnsureProfileAsync(context, student, firstName, lastName, primary.Name, "student");

      context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = primary.Id,
        UserId = student.Id,
        Role = CourseRole.Student,
        EnrolledAt = primary.StartDate.AddDays(-1)
      });

      var secondary = courses[(Array.IndexOf(courses, primary) + 1) % courses.Length];
      context.CourseEnrollments.Add(new CourseEnrollment
      {
        CourseId = secondary.Id,
        UserId = student.Id,
        Role = CourseRole.Student,
        EnrolledAt = secondary.StartDate.AddDays(-1)
      });

      seededStudents.Add((student, primary));
    }

    // SUBMISSIONS — deterministic mix so the teacher "pending" list has data.
    var random = new Random(20261014);

    foreach (var (student, course) in seededStudents)
    {
      var pastAssignments = assignments
          .Where(a => a.Course.Id == course.Id && a.Assignment.DueDate < Anchor)
          .OrderBy(a => a.Assignment.DueDate)
          .Take(2)
          .ToList();

      foreach (var (assignment, _) in pastAssignments)
      {
        var roll = random.Next(0, 4);
        var dueDay = DaysFromAnchor(assignment.DueDate);
        var (status, feedback, gradedAt) = roll switch
        {
          0 => (AssignmentStatus.Approved, "Great work, well done!", (DateTime?)D(dueDay)),
          1 => (AssignmentStatus.Revision, "Please revise section two and resubmit.", (DateTime?)D(dueDay + 1)),
          _ => (AssignmentStatus.HandedIn, string.Empty, (DateTime?)null)
        };

        context.Submissions.Add(new Submission
        {
          AssignmentId = assignment.Id,
          StudentId = student.Id,
          Content = $"Demo submission by {student.FirstName} {student.LastName} for '{assignment.Name}'.",
          Feedback = feedback,
          Status = status,
          HandinDate = D(dueDay - 1, 18, random.Next(0, 60)),
          GradedAt = gradedAt
        });
      }
    }

    // RESUBMISSIONS — give the first student in each course a full
    // hand-in → rejected → resubmitted history.
    foreach (var course in courses)
    {
      var student = seededStudents.First(s => s.Course.Id == course.Id).Student;
      var assignment = assignments
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

      original.HandinDate = D(-5, 17, 30);
      original.Status = AssignmentStatus.Revision;
      original.Feedback = "Please revise section two and resubmit.";
      original.GradedAt = D(-3, 10, 0);

      context.Submissions.Add(new Submission
      {
        AssignmentId = assignment.Id,
        StudentId = student.Id,
        Content = $"Resubmission by {student.FirstName} {student.LastName} for '{assignment.Name}'.",
        Status = AssignmentStatus.HandedIn,
        HandinDate = D(-1, 20, 15)
      });
    }

    await context.SaveChangesAsync();

    // RESOURCES — course, module and activity material plus student uploads.
    string CreatorIdFor(CourseModel course) => teacherByCourse[course].Id;

    var courseResources = new (CourseModel Course, string Name, string Url)[]
    {
      (math, "Course Syllabus & Reading List", "https://example.com/math/syllabus.pdf"),
      (math, "Formula Sheet — Calculus & Linear Algebra", "https://example.com/math/formula-sheet.pdf"),
      (history, "Course Syllabus & Assessment Guide", "https://example.com/history/syllabus.pdf"),
      (history, "World History Timeline", "https://example.com/history/timeline.pdf"),
      (cs, "Course Syllabus & Tools Setup", "https://example.com/cs/syllabus.pdf"),
      (cs, "C# Syntax Cheat Sheet", "https://example.com/cs/cheat-sheet.pdf"),
      (backend, "API Design Guide", "https://example.com/backend/api-design.pdf"),
      (backend, "Course Roadmap & Project Brief", "https://example.com/backend/roadmap.pdf")
    };

    foreach (var (course, name, url) in courseResources)
    {
      context.Resources.Add(new Resource
      {
        DisplayName = name,
        Url = url,
        CourseId = course.Id,
        CreatorId = CreatorIdFor(course),
        UploadDate = course.StartDate,
        LastEditDate = D(-5, 9, 0)
      });
    }

    // Module-level resources.
    foreach (var (module, course) in modules)
    {
      context.Resources.Add(new Resource
      {
        DisplayName = $"{module.Name} — Lecture Slides",
        Url = $"https://example.com/slides/{module.Id}.pdf",
        ModuleId = module.Id,
        CreatorId = CreatorIdFor(course),
        UploadDate = module.StartDate,
        LastEditDate = D(-3, 9, 0)
      });
    }

    // Activity-level resources.
    foreach (var (activity, course) in activities)
    {
      var resourceName = activity.ActivityType switch
      {
        ActivityType.Lecture => "Slides",
        ActivityType.Workshop => "Worksheet",
        ActivityType.Seminar => "Reading",
        ActivityType.Practice => "Exercise Set",
        ActivityType.ELearning => "Module",
        ActivityType.Mentorship => "Notes",
        _ => "Handout"
      };

      context.Resources.Add(new Resource
      {
        DisplayName = $"{activity.Name} — {resourceName}",
        Url = $"https://example.com/materials/{activity.Id}.pdf",
        ActivityId = activity.Id,
        CreatorId = CreatorIdFor(course),
        UploadDate = activity.StartDate.AddDays(-1),
        LastEditDate = D(-1, 9, 0)
      });
    }

    // Student uploads (module-attached, flagged as student submissions).
    foreach (var (student, course) in seededStudents)
    {
      var module = modules.First(m => m.Course.Id == course.Id).Module;

      context.Resources.Add(new Resource
      {
        DisplayName = $"{student.FirstName}'s project draft",
        Url = $"https://example.com/uploads/{student.Id}.pdf",
        ModuleId = module.Id,
        CreatorId = student.Id,
        IsStudentSubmitted = true,
        UploadDate = D(-random.Next(1, 12), 12, 0),
        LastEditDate = D(-random.Next(0, 5), 9, 0)
      });
    }

    await context.SaveChangesAsync();

    logger.LogInformation(
        "Seeded demo data for {CourseCount} courses, {TeacherCount} teachers and {StudentCount} students.",
        courses.Length,
        teachers.Length,
        students.Length);
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
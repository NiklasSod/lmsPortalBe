using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
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
  }

  private static async Task SeedAdminUserAsync(
      UserManager<ApplicationUser> userManager,
      IConfiguration configuration,
      ILogger logger)
  {
    var email = configuration["ADMIN_EMAIL"];
    var password = configuration["ADMIN_PASSWORD"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
      logger.LogInformation("Admin credentials not configured; skipping admin user seeding.");
      return;
    }

    var admin = await userManager.FindByEmailAsync(email);
    if (admin is null)
    {
      admin = new ApplicationUser
      {
        UserName = email,
        Email = email,
        EmailConfirmed = true
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
}

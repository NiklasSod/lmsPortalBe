using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// just a file that add role to new databases if it is not there

namespace lmsPortalBe.Data;

public static class DbSeeder
{
  private static readonly string[] DefaultRoles = { "student", "teacher" };

  public static async Task SeedRolesAsync(this IHost host)
  {
    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
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
  }
}

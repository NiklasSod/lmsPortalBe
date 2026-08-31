using System.Data.Common;
using lmsPortalBe.Data;
using lmsPortalBe.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace lmsPortalBe.Tests;

/// <summary>
/// Boots the real ASP.NET Core application against a shared in-memory SQLite
/// database so the tests exercise the full HTTP pipeline (controllers,
/// authentication, authorization and token issuance).
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
  private readonly SqliteConnection _connection;

  public TestWebApplicationFactory()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    // Program.cs reads these values from configuration and throws when they are
    // missing. DotNetEnv.Env.Load() does not overwrite variables that already
    // exist, so these take precedence over anything in the repository's .env file.
    Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "DataSource=:memory:");
    Environment.SetEnvironmentVariable(JwtConstants.Secret, "test-secret-long-enough-for-hmac-sha256-signing-key-0123456789");
    Environment.SetEnvironmentVariable(JwtConstants.Issuer, "lmsPortalBe.Tests");
    Environment.SetEnvironmentVariable(JwtConstants.Audience, "lmsPortalBe.Tests.Client");

    // Credentials used by DbSeeder to create the admin account on startup.
    Environment.SetEnvironmentVariable("ADMIN_USERNAME", "admin");
    Environment.SetEnvironmentVariable("ADMIN_EMAIL", "admin@example.com");
    Environment.SetEnvironmentVariable("ADMIN_PASSWORD", "AdminPass1");
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    builder.ConfigureServices(services =>
    {
      // Replace the connection-string based DbContext registration with one
      // that reuses the single open connection, so the in-memory database is
      // shared by migrations, seeding and every request scope.
      var dbContextDescriptor = services.SingleOrDefault(
              d => d.ServiceType == typeof(DbContextOptions<LmsPortalContext>));
      if (dbContextDescriptor is not null)
      {
        services.Remove(dbContextDescriptor);
      }

      var dbConnectionDescriptor = services.SingleOrDefault(
              d => d.ServiceType == typeof(DbConnection));
      if (dbConnectionDescriptor is not null)
      {
        services.Remove(dbConnectionDescriptor);
      }

      services.AddSingleton<DbConnection>(_ => _connection);
      services.AddDbContext<LmsPortalContext>((_, options) => options.UseSqlite(_connection));
    });
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);

    if (disposing)
    {
      _connection.Dispose();
    }
  }
}

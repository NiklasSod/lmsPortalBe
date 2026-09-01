using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace lmsPortalBe.Data
{
  /// <summary>
  /// Used only by EF Core design-time tools so that migration
  /// generation does not depend on the application's runtime
  /// configuration or secrets.
  /// 
  /// Runs on dotnet ef migrations add / dotnet ef database update
  /// </summary>
  public class LmsPortalContextFactory : IDesignTimeDbContextFactory<LmsPortalContext>
  {
    public LmsPortalContext CreateDbContext(string[] args)
    {
      var options = new DbContextOptionsBuilder<LmsPortalContext>()
          .UseSqlite("Data Source=design-time.db")
          .Options;

      return new LmsPortalContext(options);
    }
  }
}

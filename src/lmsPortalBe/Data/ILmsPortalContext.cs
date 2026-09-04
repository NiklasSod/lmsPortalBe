using lmsPortalBe.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace lmsPortalBe.Data
{
    public interface ILmsPortalContext
    {
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<CourseModel> Courses { get; set; }
        DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        DbSet<CourseModule> CourseModules { get; set; }
        DbSet<Activity> Activities { get; set; }

        DatabaseFacade Database { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

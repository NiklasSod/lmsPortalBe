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
        DbSet<Assignment> Assignments { get; set; }
        DbSet<Submission> Submissions { get; set; }
        DbSet<UserProfile> UserProfiles { get; set; }
        DbSet<Resource> Resources { get; set; }
        DbSet<Notification> Notifications { get; set; }
        DbSet<UserNotification> UserNotifications { get; set; }

        DatabaseFacade Database { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

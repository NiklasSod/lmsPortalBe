using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Data
{
    public class LmsPortalContext(DbContextOptions<LmsPortalContext> options) : IdentityDbContext<ApplicationUser>(options), ILmsPortalContext
    {
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<CourseModel> Courses { get; set; } = null!;
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; } = null!;
        public DbSet<CourseModule> CourseModules { get; set; } = null!;
        public DbSet<Activity> Activities { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<Submission> Submissions { get; set; } = null!;
        public DbSet<Resource> Resources { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Core Identity tables, named as requested.
            builder.Entity<ApplicationUser>().ToTable("lmsUser");
            builder.Entity<IdentityRole>().ToTable("lmsRole");
            builder.Entity<IdentityUserRole<string>>().ToTable("lmsUser_Role")
                .HasIndex(ur => ur.UserId)
                .IsUnique();
            builder.Entity<IdentityUserClaim<string>>().ToTable("lmsUserClaim");
            builder.Entity<IdentityUserLogin<string>>().ToTable("lmsUserLogin");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("lmsRoleClaim");
            builder.Entity<IdentityUserToken<string>>().ToTable("lmsUserToken");

            builder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("lmsRefreshToken");

                entity.HasIndex(e => e.TokenHash).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("lmsUserProfile");

                entity.HasOne(e => e.User)
                    .WithOne(u => u.Profile)
                    .HasForeignKey<UserProfile>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CourseModel>(entity =>
            {
                entity.ToTable("lmsCourse");

                entity.HasMany(e => e.Enrollments)
                    .WithOne(e => e.Course)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Modules)
                    .WithOne(e => e.Course)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CourseEnrollment>(entity =>
            {
                entity.ToTable("lmsCourseEnrollment");

                entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Enrollments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CourseModule>(entity =>
            {
                entity.ToTable("lmsCourseModule");

                entity.HasMany(e => e.Activities)
                    .WithOne(a => a.Module)
                    .HasForeignKey(e => e.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);


                entity.HasMany(e => e.Assignments)
                    .WithOne(a => a.Module)
                    .HasForeignKey(e => e.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Activity>(entity =>
            {
                entity.ToTable("lmsActivity");
            });

            builder.Entity<Resource>(entity =>
            {
                entity.ToTable("lmsResource");
                entity.HasOne(e => e.Creator)
                    .WithMany(c => c.Resources)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Resources)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Module)
                    .WithMany(c => c.Resources)
                    .HasForeignKey(e => e.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Activity)
                    .WithMany(c => c.Resources)
                    .HasForeignKey(e => e.ActivityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Assignment>(entity =>
            {
                entity.ToTable("lmsAssignment");
            });

            builder.Entity<Submission>(entity =>
            {
                entity.ToTable("lmsSubmission");

                entity.HasIndex(e => new { e.StudentId, e.AssignmentId });

                entity.HasOne(e => e.Student)
                    .WithMany(u => u.Submissions)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // A student keeps their submissions (history of handed-in work)
                // when the assignment is deleted; the link is nulled.
                entity.HasOne(e => e.Assignment)
                    .WithMany(u => u.Submissions)
                    .HasForeignKey(e => e.AssignmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}

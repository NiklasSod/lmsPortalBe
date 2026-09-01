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
            });
        }
    }
}

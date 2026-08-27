using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Data
{
    public class LmsPortalContext : IdentityDbContext<ApplicationUser>, ILmsPortalContext
    {
        public LmsPortalContext(DbContextOptions<LmsPortalContext> options) : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Prefix every table with "lms" as required for this database.
            builder.Entity<ApplicationUser>().ToTable("lmsAspNetUsers");
            builder.Entity<IdentityRole>().ToTable("lmsAspNetRoles");
            builder.Entity<IdentityUserRole<string>>().ToTable("lmsAspNetUserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("lmsAspNetUserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("lmsAspNetUserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("lmsAspNetRoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("lmsAspNetUserTokens");

            builder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("lmsRefreshTokens");

                entity.HasIndex(e => e.TokenHash).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

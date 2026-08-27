using lmsPortalBe.Models;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Data
{
    public interface ILmsPortalContext
    {
        DbSet<RefreshToken> RefreshTokens { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

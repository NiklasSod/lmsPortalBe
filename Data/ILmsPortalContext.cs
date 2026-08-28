using lmsPortalBe.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace lmsPortalBe.Data
{
    public interface ILmsPortalContext
    {
        DbSet<RefreshToken> RefreshTokens { get; set; }

        DatabaseFacade Database { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

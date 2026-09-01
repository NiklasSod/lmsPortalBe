using lmsPortalBe.Models;

namespace lmsPortalBe.Services
{
    public interface ITokenService
    {
        string CreateAccessToken(ApplicationUser user, IList<string> roles);

        Task<AuthTokens> CreateTokensAsync(ApplicationUser user, IList<string> roles);

        Task<AuthTokens> RefreshTokenAsync(string refreshToken);

        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}

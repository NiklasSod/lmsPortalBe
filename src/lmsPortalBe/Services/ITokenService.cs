using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.Models;

namespace lmsPortalBe.Services
{
    public interface ITokenService
    {
        string CreateAccessToken(ApplicationUser user, IList<string> roles);

        Task<AuthResponseDto> CreateTokensAsync(ApplicationUser user, IList<string> roles);

        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);

        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}

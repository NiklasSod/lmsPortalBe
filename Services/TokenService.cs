using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using lmsPortalBe.Data;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace lmsPortalBe.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILmsPortalContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(
        IConfiguration configuration,
        ILmsPortalContext context,
        UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _context = context;
        _userManager = userManager;
    }

    private string Secret => _configuration[JwtConstants.Secret]
        ?? throw new InvalidOperationException($"Missing configuration value '{JwtConstants.Secret}'.");
    private string Issuer => _configuration[JwtConstants.Issuer] ?? "lmsPortalBe";
    private string Audience => _configuration[JwtConstants.Audience] ?? "lmsPortalBe";
    private TimeSpan AccessTokenLifetime =>
        TimeSpan.FromMinutes(GetInt(JwtConstants.AccessTokenMinutes, JwtConstants.DefaultAccessTokenMinutes));
    private TimeSpan RefreshTokenLifetime =>
        TimeSpan.FromDays(GetInt(JwtConstants.RefreshTokenDays, JwtConstants.DefaultRefreshTokenDays));

    private int GetInt(string key, int fallback) =>
        int.TryParse(_configuration[key], out var value) ? value : fallback;

    public string CreateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto> CreateTokensAsync(ApplicationUser user, IList<string> roles)
    {
        var accessToken = CreateAccessToken(user, roles);
        var refreshToken = CreateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = HashRefreshToken(refreshToken),
            UserId = user.Id,
            Expires = GetRefreshTokenExpiry(),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.Add(AccessTokenLifetime)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken is null || storedToken.Expires <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Reuse detection: presenting an already-rotated/revoked token is a strong
        // signal of theft, so revoke every active token for that user.
        if (storedToken.IsRevoked)
        {
            await RevokeAllForUserAsync(storedToken.UserId);
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user is null)
        {
            throw new UnauthorizedAccessException("User not found for refresh token.");
        }

        // Revoke the old token and issue the new pair atomically so a failure
        // cannot leave the user with a revoked token and no replacement.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            storedToken.IsRevoked = true;

            var roles = await _userManager.GetRolesAsync(user);
            var response = await CreateTokensAsync(user, roles);

            await transaction.CommitAsync();
            return response;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task RevokeAllForUserAsync(string userId)
    {
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true));
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken is not null && !storedToken.IsRevoked)
        {
            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }

    private static string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    private DateTime GetRefreshTokenExpiry()
    {
        return DateTime.UtcNow.Add(RefreshTokenLifetime);
    }
}

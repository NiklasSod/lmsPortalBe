using AutoMapper;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.Models;
using lmsPortalBe.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace lmsPortalBe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IMapper mapper,
        IWebHostEnvironment environment) : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IMapper _mapper = mapper;
        private readonly IWebHostEnvironment _environment = environment;

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto)
        {
            var user = _mapper.Map<ApplicationUser>(dto);

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "student");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(roleResult.Errors.Select(e => e.Description));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokens = await _tokenService.CreateTokensAsync(user, roles);

            return IssueTokens(tokens);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return Unauthorized("Invalid email or password.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokens = await _tokenService.CreateTokensAsync(user, roles);

            return IssueTokens(tokens);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh()
        {
            var refreshToken = Request.Cookies[JwtConstants.RefreshTokenCookie];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized("Missing refresh token.");
            }

            try
            {
                var tokens = await _tokenService.RefreshTokenAsync(refreshToken);
                return IssueTokens(tokens);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[JwtConstants.RefreshTokenCookie];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _tokenService.RevokeRefreshTokenAsync(refreshToken);
            }

            Response.Cookies.Delete(JwtConstants.RefreshTokenCookie, BuildCookieOptions());
            return NoContent();
        }

        private AuthResponseDto IssueTokens(AuthTokens tokens)
        {
            SetRefreshTokenCookie(tokens);

            return new AuthResponseDto
            {
                AccessToken = tokens.AccessToken,
                ExpiresAt = tokens.ExpiresAt
            };
        }

        private void SetRefreshTokenCookie(AuthTokens tokens)
        {
            Response.Cookies.Append(
                JwtConstants.RefreshTokenCookie,
                tokens.RefreshToken,
                BuildCookieOptions(tokens.RefreshTokenExpiresAt));
        }

        private CookieOptions BuildCookieOptions(DateTime? expires = null) => new()
        {
            HttpOnly = true,
            Secure = _environment.IsProduction(),
            // In production the SPA and API usually live on different origins, so the
            // cookie has to be sent cross-site (None). In development (Vite on
            // localhost) Strict keeps the same-site cookie tight.
            SameSite = _environment.IsProduction() ? SameSiteMode.None : SameSiteMode.Strict,
            Path = "/",
            Expires = expires.HasValue ? new DateTimeOffset(expires.Value) : null
        };
    }
}

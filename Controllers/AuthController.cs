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
        IMapper mapper) : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IMapper _mapper = mapper;

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto)
        {
            var user = _mapper.Map<ApplicationUser>(dto);

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(roleResult.Errors.Select(e => e.Description));
            }

            var roles = await _userManager.GetRolesAsync(user);
            return await _tokenService.CreateTokensAsync(user, roles);
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
            return await _tokenService.CreateTokensAsync(user, roles);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshRequestDto dto)
        {
            try
            {
                return await _tokenService.RefreshTokenAsync(dto.RefreshToken);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshRequestDto dto)
        {
            await _tokenService.RevokeRefreshTokenAsync(dto.RefreshToken);
            return NoContent();
        }
    }
}

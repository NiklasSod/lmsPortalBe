using AutoMapper;
using lmsPortalBe.Data;
using lmsPortalBe.DTOs.UserProfile;
using lmsPortalBe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Controllers
{
  [ApiController]
  [Route("api/profiles")]
  [Authorize]
  public class UserProfileController(
      ILmsPortalContext context,
      IMapper mapper,
      UserManager<ApplicationUser> userManager)
      : CoursePortalControllerBase(context, mapper)
  {
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
      var profile = await _context.UserProfiles
          .FirstOrDefaultAsync(p => p.UserId == CurrentUserId);

      if (profile is null)
      {
        return NotFound("Profile not found.");
      }

      return Ok(_mapper.Map<UserProfileDto>(profile));
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile(string userId)
    {
      var profile = await _context.UserProfiles
          .FirstOrDefaultAsync(p => p.UserId == userId);

      if (profile is null)
      {
        return NotFound("Profile not found.");
      }

      return Ok(_mapper.Map<UserProfileDto>(profile));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProfile(CreateUserProfileRequestDto dto)
    {
      var userId = CurrentUserId;

      var user = await _userManager.FindByIdAsync(userId);
      if (user is null)
      {
        return Unauthorized("User not found.");
      }

      if (await _context.UserProfiles.AnyAsync(p => p.UserId == userId))
      {
        return Conflict("Profile already exists. Use PATCH to update it.");
      }

      var profile = new UserProfile
      {
        UserId = userId,
        AboutMe = dto.AboutMe,
        GitHubLink = dto.GitHubLink,
        Skills = dto.Skills ?? [],
        WhatsAppNumber = dto.WhatsAppNumber,
        DateOfBirth = dto.DateOfBirth
      };

      _context.UserProfiles.Add(profile);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetMyProfile), _mapper.Map<UserProfileDto>(profile));
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateProfile(UpdateUserProfileRequestDto dto)
    {
      var profile = await _context.UserProfiles
          .FirstOrDefaultAsync(p => p.UserId == CurrentUserId);

      if (profile is null)
      {
        return NotFound("Profile not found. Create one with POST first.");
      }

      if (dto.AboutMe is not null)
      {
        profile.AboutMe = dto.AboutMe;
      }

      if (dto.GitHubLink is not null)
      {
        profile.GitHubLink = dto.GitHubLink;
      }

      if (dto.Skills is not null)
      {
        profile.Skills = dto.Skills;
      }

      if (dto.WhatsAppNumber is not null)
      {
        profile.WhatsAppNumber = dto.WhatsAppNumber;
      }

      if (dto.DateOfBirth is not null)
      {
        profile.DateOfBirth = dto.DateOfBirth;
      }

      await _context.SaveChangesAsync();

      return Ok(_mapper.Map<UserProfileDto>(profile));
    }
  }
}

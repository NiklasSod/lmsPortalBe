using AutoMapper;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.MappingProfiles;
using lmsPortalBe.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace lmsPortalBe.Tests;

public class AutoMapperProfileTests
{
  [Fact]
  public void RegisterRequestDto_MapsUserNameFromEmail()
  {
    var configuration = new MapperConfiguration(
        cfg => cfg.AddProfile<AutoMapperProfile>(),
        NullLoggerFactory.Instance);
    var mapper = configuration.CreateMapper();

    var dto = new RegisterRequestDto
    {
      FirstName = "John",
      LastName = "Smith",
      Email = "john.smith@example.com",
      Password = "Passw0rd1"
    };

    var user = mapper.Map<ApplicationUser>(dto);

    Assert.Equal(dto.Email, user.UserName);
    Assert.Equal(dto.FirstName, user.FirstName);
    Assert.Equal(dto.LastName, user.LastName);
    Assert.Null(user.PasswordHash);
  }
}

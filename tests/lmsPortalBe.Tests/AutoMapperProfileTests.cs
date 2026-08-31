using AutoMapper;
using lmsPortalBe.MappingProfiles;
using Microsoft.Extensions.Logging.Abstractions;

namespace lmsPortalBe.Tests;

public class AutoMapperProfileTests
{
  [Fact]
  public void AutoMapper_Configuration_IsValid()
  {
    var configuration = new MapperConfiguration(
        cfg => cfg.AddProfile<AutoMapperProfile>(),
        NullLoggerFactory.Instance);

    configuration.AssertConfigurationIsValid();
  }
}

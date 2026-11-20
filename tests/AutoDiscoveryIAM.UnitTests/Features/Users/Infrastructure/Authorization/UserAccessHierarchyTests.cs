using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Infrastructure.Authorization;

public class UserAccessHierarchyTests
{
  [Theory(DisplayName = "CanManage exige un nivel igual o superior al de la cuenta destino")]
  [InlineData("none", "none", true)]
  [InlineData("none", null, true)]
  [InlineData("admin", "none", false)]
  [InlineData("admin", "admin", false)]
  [InlineData("admin", "su", true)]
  [InlineData("admin", "dev", true)]
  [InlineData("su", "admin", false)]
  [InlineData("su", "su", true)]
  [InlineData("su", "dev", true)]
  [InlineData("dev", "su", false)]
  [InlineData("dev", "dev", true)]
  [InlineData("dev", null, false)]
  public void CanManage_RequiresEqualOrHigherLevel(string targetLevel, string? callerLevel, bool expected)
  {
    var target = WithLevel(new User(), targetLevel);
    var caller = callerLevel is null ? null : WithLevel(new User(), callerLevel);

    UserAccessHierarchy.CanManage(caller, target).Should().Be(expected);
  }

  private static User WithLevel(User user, string level)
  {
    user.IsAdmin = level == "admin";
    user.IsSuAdmin = level == "su";
    user.IsDevelopment = level == "dev";
    return user;
  }
}

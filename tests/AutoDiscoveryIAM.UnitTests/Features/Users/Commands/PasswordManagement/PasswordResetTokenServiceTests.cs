using AwesomeAssertions;
using NSubstitute;
using Xunit;
using Microsoft.Extensions.Options;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.PasswordManagement;

public class PasswordResetTokenServiceTests
{
  private static PasswordResetTokenService CreateSut(int tokenLength = 64)
  {
    var settings = new PasswordResetSettings { TokenLength = tokenLength };
    var options = Substitute.For<IOptions<PasswordResetSettings>>();
    options.Value.Returns(settings);
    return new PasswordResetTokenService(options);
  }

  [Fact(DisplayName = "GenerateToken retorna un token con la longitud configurada")]
  public void GenerateToken_ReturnsTokenWithConfiguredLength()
  {
    var sut = CreateSut(64);

    var token = sut.GenerateToken();

    token.Length.Should().Be(64);
  }

  [Fact(DisplayName = "GenerateToken respeta un minimo de 32 caracteres aunque la configuracion sea menor")]
  public void GenerateToken_EnforcesMinimumLength()
  {
    var sut = CreateSut(8);

    var token = sut.GenerateToken();

    token.Length.Should().Be(32);
  }

  [Fact(DisplayName = "GenerateToken produce valores distintos en llamadas consecutivas")]
  public void GenerateToken_ProducesUniqueValues()
  {
    var sut = CreateSut();

    var first = sut.GenerateToken();
    var second = sut.GenerateToken();

    first.Should().NotBe(second);
  }

  [Fact(DisplayName = "GenerateToken no contiene caracteres no seguros para URL")]
  public void GenerateToken_DoesNotContainUrlUnsafeCharacters()
  {
    var sut = CreateSut();

    var token = sut.GenerateToken();

    token.Should().NotContain("+");
    token.Should().NotContain("/");
    token.Should().NotContain("=");
  }
}

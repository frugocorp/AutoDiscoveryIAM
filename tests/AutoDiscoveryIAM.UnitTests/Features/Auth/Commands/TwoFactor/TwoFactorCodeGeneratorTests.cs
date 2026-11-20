using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.TwoFactor;

public class TwoFactorCodeGeneratorTests
{
  private readonly TwoFactorCodeGenerator _sut = new();

  [Fact(DisplayName = "GenerateChallengeToken retorna un valor no vacio y distinto en cada llamada")]
  public void GenerateChallengeToken_ReturnsUniqueNonEmptyValues()
  {
    var first = _sut.GenerateChallengeToken();
    var second = _sut.GenerateChallengeToken();

    first.Should().NotBeNullOrWhiteSpace();
    second.Should().NotBeNullOrWhiteSpace();
    first.Should().NotBe(second);
  }

  [Theory(DisplayName = "GenerateNumericCode retorna un codigo numerico con la longitud solicitada")]
  [InlineData(4)]
  [InlineData(6)]
  [InlineData(8)]
  public void GenerateNumericCode_ReturnsCodeWithRequestedLength(int length)
  {
    var code = _sut.GenerateNumericCode(length);

    code.Length.Should().Be(length);
    code.All(char.IsDigit).Should().BeTrue();
  }
}

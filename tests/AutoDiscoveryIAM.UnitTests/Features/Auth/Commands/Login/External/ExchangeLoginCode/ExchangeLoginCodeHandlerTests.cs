using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.External.ExchangeLoginCode;

public class ExchangeLoginCodeHandlerTests
{
  private readonly IExternalLoginExchangeCodeRepository _repository;
  private readonly ExchangeLoginCodeHandler _handler;

  public ExchangeLoginCodeHandlerTests()
  {
    _repository = Substitute.For<IExternalLoginExchangeCodeRepository>();
    _handler = new ExchangeLoginCodeHandler(_repository);
  }

  [Fact]
  public async Task Handle_WhenCodeIsValid_ShouldReturnSession()
  {
    var session = new LoginResponseDto("jwt-access-token", DateTime.UtcNow.AddMinutes(15), "plaintext-refresh-token");
    _repository.ConsumeAsync("valid-code", Arg.Any<CancellationToken>()).Returns(session);

    var result = await _handler.Handle(new ExchangeLoginCodeCommand("valid-code"), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(session);
  }

  [Fact]
  public async Task Handle_WhenCodeIsInvalidExpiredOrAlreadyConsumed_ShouldFailWithUnauthorized()
  {
    _repository.ConsumeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((LoginResponseDto?)null);

    var result = await _handler.Handle(new ExchangeLoginCodeCommand("stale-or-unknown-code"), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ErrorInfo.Status401Unauthorized);
  }
}

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.CreateUserRoles;

public class CreateUserRolesHandlerTests
{
  private readonly ICreateUserRolesService _service;
  private readonly CreateUserRolesHandler _handler;

  public CreateUserRolesHandlerTests()
  {
    _service = Substitute.For<ICreateUserRolesService>();
    _handler = new CreateUserRolesHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var userPublicId = Guid.NewGuid();
    var rolePublicId = Guid.NewGuid();
    var expected = new CreateUserRolesResponseDto(userPublicId, rolePublicId, DateTime.UtcNow);

    _service.ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(expected));

    var result = await _handler.Handle(new CreateUserRolesCommand(userPublicId, rolePublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var userPublicId = Guid.NewGuid();
    var rolePublicId = Guid.NewGuid();
    const string error = "Relacion ya asignada";

    _service.ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<CreateUserRolesResponseDto>(ErrorInfo.Status422Business, error));

    var result = await _handler.Handle(new CreateUserRolesCommand(userPublicId, rolePublicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be(error);
  }
}

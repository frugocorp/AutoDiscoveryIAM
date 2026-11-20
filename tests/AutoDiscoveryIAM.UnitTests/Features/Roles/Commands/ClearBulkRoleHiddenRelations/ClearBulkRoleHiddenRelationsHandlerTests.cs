using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public class ClearBulkRoleHiddenRelationsHandlerTests
{
  private readonly IClearBulkRoleHiddenRelationsService _service;
  private readonly ClearBulkRoleHiddenRelationsHandler _handler;

  public ClearBulkRoleHiddenRelationsHandlerTests()
  {
    _service = Substitute.For<IClearBulkRoleHiddenRelationsService>();
    _handler = new ClearBulkRoleHiddenRelationsHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var rolePublicId = Guid.NewGuid();
    var expected = new ClearBulkRoleHiddenRelationsResponseDto(
      RolePublicId: rolePublicId,
      ClearedUserRelations: 2,
      ClearedPermissionRelations: 3,
      TotalCleared: 5,
      ExecutedAt: DateTime.UtcNow);

    _service.ExecuteAsync(rolePublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(expected));

    var result = await _handler.Handle(new ClearBulkRoleHiddenRelationsCommand(rolePublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(rolePublicId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var rolePublicId = Guid.NewGuid();
    const string error = "Rol no encontrado";

    _service.ExecuteAsync(rolePublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<ClearBulkRoleHiddenRelationsResponseDto>(ErrorInfo.Status404NotFound, error));

    var result = await _handler.Handle(new ClearBulkRoleHiddenRelationsCommand(rolePublicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(error);
  }
}

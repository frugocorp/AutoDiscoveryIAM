using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetUserByPublicId;

public class GetUserByPublicIdHandlerTests
{
  private readonly IGetUserByPublicIdRepository _repository;
  private readonly IUserLogger _logger;
  private readonly GetUserByPublicIdHandler _handler;

  public GetUserByPublicIdHandlerTests()
  {
    _repository = Substitute.For<IGetUserByPublicIdRepository>();
    _logger = Substitute.For<IUserLogger>();
    _handler = new GetUserByPublicIdHandler(_repository, _logger);
  }

  [Fact]
  public async Task Handle_WhenQueryIsNull_ShouldThrowArgumentNullException()
  {
    await ((Func<Task>)(() => _handler.Handle(null!, CancellationToken.None))).Should().ThrowAsync<ArgumentNullException>();
  }

  [Fact]
  public async Task Handle_WhenUserDoesNotExist_ShouldReturnNotFound()
  {
    var publicId = Guid.NewGuid();
    _repository.GetUserByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

    var result = await _handler.Handle(new GetUserByPublicIdQuery(publicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Contain(publicId.ToString());
    _logger.Received(1).LogUserNotFoundByPublicId(publicId);
  }

  [Fact]
  public async Task Handle_WhenUserExists_ShouldReturnMappedDto()
  {
    var user = new User
    {
      Id = 7,
      PublicId = Guid.NewGuid(),
      Username = "john",
      Email = "john@test.local",
      PasswordHash = "hash",
      IsActive = true,
    };

    _repository.GetUserByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

    var result = await _handler.Handle(new GetUserByPublicIdQuery(user.PublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.PublicId.Should().Be(user.PublicId);
    result.Data.Username.Should().Be(user.Username);
    _logger.Received(1).LogUserRetrieved(user.Id);
  }
}

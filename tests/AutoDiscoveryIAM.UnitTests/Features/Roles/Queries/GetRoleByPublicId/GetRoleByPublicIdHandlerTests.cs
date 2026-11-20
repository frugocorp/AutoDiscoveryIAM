using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetRoleByPublicId;

public class GetRoleByPublicIdHandlerTests
{
    private readonly IGetRoleByPublicIdRepository _repository;
    private readonly IRoleLogger _logger;
    private readonly GetRoleByPublicIdHandler _handler;

    public GetRoleByPublicIdHandlerTests()
    {
        _repository = Substitute.For<IGetRoleByPublicIdRepository>();
        _logger = Substitute.For<IRoleLogger>();
        _handler = new GetRoleByPublicIdHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WhenRoleDoesNotExist_ShouldReturnNotFound()
    {
        var publicId = Guid.NewGuid();
        _repository.GetRoleByPublicId(publicId).Returns((Role?)null);

        var result = await _handler.Handle(new GetRoleByPublicIdQuery(publicId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Contain(publicId.ToString());
        _logger.Received(1).LogRoleNotFoundByPublicId(publicId);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_ShouldReturnMappedDto()
    {
        var role = new Role
        {
            PublicId = Guid.NewGuid(),
            Name = "operator",
            Description = "rol operador",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _repository.GetRoleByPublicId(role.PublicId).Returns(role);

        var result = await _handler.Handle(new GetRoleByPublicIdQuery(role.PublicId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PublicId.Should().Be(role.PublicId);
        result.Data.Name.Should().Be(role.Name);
        _logger.Received(1).LogRoleRetrievedByPublicId(role.PublicId);
    }
}

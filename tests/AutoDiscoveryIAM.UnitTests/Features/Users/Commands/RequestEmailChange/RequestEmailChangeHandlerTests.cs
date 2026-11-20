using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.RequestEmailChange;

public class RequestEmailChangeHandlerTests
{
    private static (RequestEmailChangeHandler Handler, IRequestEmailChangeRepository Repository, IAutoDiscoveryIAMUnitOfWork AutoDiscoveryIAMUnitOfWork, IUserLogger Logger) CreateSut()
    {
        var repository = Substitute.For<IRequestEmailChangeRepository>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var logger = Substitute.For<IUserLogger>();
        var handler = new RequestEmailChangeHandler(repository, unitOfWork, logger);
        return (handler, repository, unitOfWork, logger);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
    {
        var (handler, repository, _, _) = CreateSut();
        var publicId = Guid.NewGuid();
        repository.GetUserByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await handler.Handle(new RequestEmailChangeCommand(publicId, "new@example.com"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task Handle_WhenNewEmailSameAsCurrent_ShouldReturnBusinessError()
    {
        var (handler, repository, _, _) = CreateSut();
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "same@example.com" };
        repository.GetUserByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await handler.Handle(new RequestEmailChangeCommand(user.PublicId, "same@example.com"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    }

    [Fact(DisplayName = "Un email ya registrado no se delata: la solicitud queda pendiente igual que con uno libre")]
    public async Task Handle_WhenNewEmailAlreadyInUse_ShouldNotRevealIt()
    {
        var (handler, repository, _, _) = CreateSut();
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "old@example.com" };
        repository.GetUserByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
        repository.ExistsEmailAsync("new@example.com", user.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(new RequestEmailChangeCommand(user.PublicId, "new@example.com"), CancellationToken.None);

        result.Success.Should().BeTrue();
        await repository.Received(1).AddAsync(Arg.Any<EmailChangeRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPendingRequestAlreadyExists_ShouldReturnConflict()
    {
        var (handler, repository, _, _) = CreateSut();
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "old@example.com" };
        repository.GetUserByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
        repository.ExistsEmailAsync("new@example.com", user.Id, Arg.Any<CancellationToken>()).Returns(false);
        repository.HasPendingRequestAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(new RequestEmailChangeCommand(user.PublicId, "new@example.com"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreatePendingRequest()
    {
        var (handler, repository, unitOfWork, logger) = CreateSut();
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "old@example.com" };
        repository.GetUserByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
        repository.ExistsEmailAsync("new@example.com", user.Id, Arg.Any<CancellationToken>()).Returns(false);
        repository.HasPendingRequestAsync(user.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.Handle(new RequestEmailChangeCommand(user.PublicId, "new@example.com"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.RequestedEmail.Should().Be("new@example.com");
        result.Data.Status.Should().Be(EmailChangeRequestStatusCatalog.Pending);

        await repository.Received(1).AddAsync(Arg.Is<EmailChangeRequest>(r => r.UserId == user.Id && r.RequestedEmail == "new@example.com"), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        logger.Received(1).LogEmailChangeRequested(user.Id, "new@example.com");
    }
}

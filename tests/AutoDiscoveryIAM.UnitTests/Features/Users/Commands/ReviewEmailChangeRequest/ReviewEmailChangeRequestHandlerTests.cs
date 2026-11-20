using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.ReviewEmailChangeRequest;

public class ReviewEmailChangeRequestHandlerTests
{
    private static (ReviewEmailChangeRequestHandler Handler, IReviewEmailChangeRequestRepository Repository, IAutoDiscoveryIAMUnitOfWork AutoDiscoveryIAMUnitOfWork, IAuthContextService AuthContext, IUserLogger Logger) CreateSut(long reviewerId = 99)
    {
        var repository = Substitute.For<IReviewEmailChangeRequestRepository>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var authContext = Substitute.For<IAuthContextService>();
        authContext.GetCurrentUserAsync().Returns(new User { Id = reviewerId, PublicId = Guid.NewGuid(), Email = "admin@example.com" });
        var logger = Substitute.For<IUserLogger>();
        var handler = new ReviewEmailChangeRequestHandler(repository, unitOfWork, authContext, logger);
        return (handler, repository, unitOfWork, authContext, logger);
    }

    [Fact]
    public async Task Handle_WhenRequestNotFound_ShouldReturnNotFound()
    {
        var (handler, repository, _, _, _) = CreateSut();
        var publicId = Guid.NewGuid();
        repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((EmailChangeRequest?)null);

        var result = await handler.Handle(new ReviewEmailChangeRequestCommand(publicId, true, null), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task Handle_WhenRequestAlreadyReviewed_ShouldReturnConflict()
    {
        var (handler, repository, _, _, _) = CreateSut();
        var request = new EmailChangeRequest { Id = 1, PublicId = Guid.NewGuid(), Status = EmailChangeRequestStatusCatalog.Rejected };
        repository.GetByPublicIdAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);

        var result = await handler.Handle(new ReviewEmailChangeRequestCommand(request.PublicId, true, null), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }

    [Fact]
    public async Task Handle_WhenApproved_ShouldSetApprovedForExecution()
    {
        var (handler, repository, unitOfWork, _, logger) = CreateSut(reviewerId: 42);
        var request = new EmailChangeRequest { Id = 1, PublicId = Guid.NewGuid(), Status = EmailChangeRequestStatusCatalog.Pending };
        repository.GetByPublicIdAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);

        var result = await handler.Handle(new ReviewEmailChangeRequestCommand(request.PublicId, true, null), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(EmailChangeRequestStatusCatalog.ApprovedForExecution);
        request.ReviewedBy.Should().Be(42);
        request.ReviewedAt.Should().NotBeNull();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        logger.Received(1).LogEmailChangeReviewed(request.Id, 42, true);
    }

    [Fact]
    public async Task Handle_WhenRejected_ShouldSetRejectedWithReason()
    {
        var (handler, repository, unitOfWork, _, logger) = CreateSut(reviewerId: 42);
        var request = new EmailChangeRequest { Id = 1, PublicId = Guid.NewGuid(), Status = EmailChangeRequestStatusCatalog.Pending };
        repository.GetByPublicIdAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);

        var result = await handler.Handle(new ReviewEmailChangeRequestCommand(request.PublicId, false, "correo sospechoso"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(EmailChangeRequestStatusCatalog.Rejected);
        result.Data.RejectionReason.Should().Be("correo sospechoso");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        logger.Received(1).LogEmailChangeReviewed(request.Id, 42, false);
    }

    [Fact]
    public async Task Handle_WhenConcurrentReviewConflictOccurs_ShouldReturnConflict()
    {
        var (handler, repository, unitOfWork, _, _) = CreateSut(reviewerId: 42);
        var request = new EmailChangeRequest { Id = 1, PublicId = Guid.NewGuid(), Status = EmailChangeRequestStatusCatalog.Pending };
        repository.GetByPublicIdAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new DbUpdateConcurrencyException());

        var result = await handler.Handle(new ReviewEmailChangeRequestCommand(request.PublicId, true, null), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }
}

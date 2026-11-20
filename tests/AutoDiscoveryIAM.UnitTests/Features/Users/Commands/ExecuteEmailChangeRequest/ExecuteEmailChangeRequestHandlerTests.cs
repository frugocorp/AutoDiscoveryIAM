using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.ExecuteEmailChangeRequest;

public class ExecuteEmailChangeRequestHandlerTests
{
    private static (
        ExecuteEmailChangeRequestHandler Handler,
        IExecuteEmailChangeRequestRepository Repository,
        IAutoDiscoveryIAMUnitOfWork AutoDiscoveryIAMUnitOfWork,
        IActivationService ActivationService,
        IActivationEmailService ActivationEmailService,
        IUserLogger Logger) CreateSut(long executorId = 7)
    {
        var repository = Substitute.For<IExecuteEmailChangeRequestRepository>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var authContext = Substitute.For<IAuthContextService>();
        authContext.GetCurrentUserAsync().Returns(new User { Id = executorId, PublicId = Guid.NewGuid(), Email = "su@example.com" });
        var activationService = Substitute.For<IActivationService>();
        var activationEmailService = Substitute.For<IActivationEmailService>();
        activationEmailService
            .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Task.FromResult(OperationResult.Ok(true)));
        var logger = Substitute.For<IUserLogger>();

        var handler = new ExecuteEmailChangeRequestHandler(repository, unitOfWork, authContext, activationService, activationEmailService, logger);
        return (handler, repository, unitOfWork, activationService, activationEmailService, logger);
    }

    private static EmailChangeRequest CreateApprovedRequest(User user) => new()
    {
        Id = 1,
        PublicId = Guid.NewGuid(),
        UserId = user.Id,
        RequestedEmail = "new@example.com",
        Status = EmailChangeRequestStatusCatalog.ApprovedForExecution,
        User = user,
    };

    [Fact]
    public async Task Handle_WhenRequestNotFound_ShouldReturnNotFound()
    {
        var (handler, repository, _, _, _, _) = CreateSut();
        var publicId = Guid.NewGuid();
        repository.GetByPublicIdWithUserAsync(publicId, Arg.Any<CancellationToken>()).Returns((EmailChangeRequest?)null);

        var result = await handler.Handle(new ExecuteEmailChangeRequestCommand(publicId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task Handle_WhenRequestNotApproved_ShouldReturnConflict()
    {
        var (handler, repository, _, _, _, _) = CreateSut();
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "old@example.com" };
        var request = CreateApprovedRequest(user);
        request.Status = EmailChangeRequestStatusCatalog.Pending;
        repository.GetByPublicIdWithUserAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);

        var result = await handler.Handle(new ExecuteEmailChangeRequestCommand(request.PublicId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }

    [Fact]
    public async Task Handle_WhenEmailNowTakenByAnotherUser_ShouldReturnConflict()
    {
        var (handler, repository, _, _, _, _) = CreateSut();
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "old@example.com" };
        var request = CreateApprovedRequest(user);
        repository.GetByPublicIdWithUserAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);
        repository.ExistsEmailAsync(request.RequestedEmail, user.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(new ExecuteEmailChangeRequestCommand(request.PublicId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldChangeEmailResetVerificationAndMarkExecuted()
    {
        var (handler, repository, unitOfWork, activationService, activationEmailService, logger) = CreateSut(executorId: 7);
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "old@example.com", IsEmailVerified = true };
        var request = CreateApprovedRequest(user);
        repository.GetByPublicIdWithUserAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);
        repository.ExistsEmailAsync(request.RequestedEmail, user.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.Handle(new ExecuteEmailChangeRequestCommand(request.PublicId), CancellationToken.None);

        result.Success.Should().BeTrue();
        user.Email.Should().Be("new@example.com");
        user.IsEmailVerified.Should().BeFalse();
        request.Status.Should().Be(EmailChangeRequestStatusCatalog.Executed);
        request.ExecutedBy.Should().Be(7);
        request.ExecutedAt.Should().NotBeNull();

        activationService.Received(1).AssignActivationToken(user);
        await activationEmailService.Received(1).SendActivationEmailAsync(user, Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        logger.Received(1).LogEmailChangeExecuted(request.Id, user.Id, 7);
    }

    [Fact]
    public async Task Handle_WhenActivationEmailFails_ShouldReturnDependencyErrorAndNotMarkExecuted()
    {
        var (handler, repository, unitOfWork, _, activationEmailService, _) = CreateSut();
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "old@example.com" };
        var request = CreateApprovedRequest(user);
        repository.GetByPublicIdWithUserAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);
        repository.ExistsEmailAsync(request.RequestedEmail, user.Id, Arg.Any<CancellationToken>()).Returns(false);
        activationEmailService
            .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Task.FromResult(OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "smtp error")));

        var result = await handler.Handle(new ExecuteEmailChangeRequestCommand(request.PublicId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
        request.Status.Should().Be(EmailChangeRequestStatusCatalog.ApprovedForExecution);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConcurrentExecutionConflictOccurs_ShouldReturnConflict()
    {
        var (handler, repository, unitOfWork, _, _, _) = CreateSut();
        var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "old@example.com" };
        var request = CreateApprovedRequest(user);
        repository.GetByPublicIdWithUserAsync(request.PublicId, Arg.Any<CancellationToken>()).Returns(request);
        repository.ExistsEmailAsync(request.RequestedEmail, user.Id, Arg.Any<CancellationToken>()).Returns(false);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new DbUpdateConcurrencyException());

        var result = await handler.Handle(new ExecuteEmailChangeRequestCommand(request.PublicId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }
}

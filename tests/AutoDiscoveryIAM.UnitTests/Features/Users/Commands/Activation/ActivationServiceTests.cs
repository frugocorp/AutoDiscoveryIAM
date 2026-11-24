using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.Activation;

public class ActivationServiceTests
{
    private static (
        ActivationService Service,
        IActivationRepository Repository,
        IActivationTokenService TokenService,
        IActivationEmailService EmailService,
        IAutoDiscoveryIAMUnitOfWork AutoDiscoveryIAMUnitOfWork) CreateSut(ActivationSettings? settings = null, string? baseUrl = "https://app.example.com")
    {
        var repository = Substitute.For<IActivationRepository>();
        var tokenService = Substitute.For<IActivationTokenService>();
        var emailService = Substitute.For<IActivationEmailService>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var options = Options.Create(settings ?? new ActivationSettings());
        var configuration = Substitute.For<IConfiguration>();
        configuration["BaseUrl"].Returns(baseUrl);
        var logger = Substitute.For<ILogger<ActivationService>>();

        var service = new ActivationService(repository, tokenService, emailService, unitOfWork, options, configuration, logger, Substitute.For<IAuthContextService>());
        return (service, repository, tokenService, emailService, unitOfWork);
    }

    // ───────────────────────────────────────────────────────────────────────
    // AssignActivationToken
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void AssignActivationToken_SetsTokenExpirationAndResetsResendWindow()
    {
        var (service, _, tokenService, _, _) = CreateSut(new ActivationSettings { EmailVerificationTokenExpiryMinutes = 45 });
        tokenService.GenerateToken().Returns("generated-token");
        var user = new User { Email = "u@example.com" };

        var token = service.AssignActivationToken(user);

        token.Should().Be("generated-token", "el token en claro es el que va en el correo");
        user.EmailVerificationToken.Should().Be(OneTimeTokenHasher.Hash("generated-token"), "en la base de datos solo queda el hash");
        user.EmailVerificationTokenExpiration.Should().NotBeNull();
        user.EmailVerificationLastSentAt.Should().NotBeNull();
        user.EmailVerificationResendWindowStartAt.Should().NotBeNull();
        user.EmailVerificationResendCount.Should().Be(1);
    }

    // ───────────────────────────────────────────────────────────────────────
    // ConfirmByTokenAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmByTokenAsync_WhenTokenIsBlank_ReturnsValidationError()
    {
        var (service, _, _, _, _) = CreateSut();

        var result = await service.ConfirmByTokenAsync("   ", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
    }

    [Fact]
    public async Task ConfirmByTokenAsync_WhenUserNotFound_ReturnsNotFound()
    {
        var (service, repository, _, _, _) = CreateSut();
        repository.GetByTokenHashAsync(OneTimeTokenHasher.Hash("abc"), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await service.ConfirmByTokenAsync("abc", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task ConfirmByTokenAsync_WhenTokenExpired_ReturnsConflict()
    {
        var (service, repository, _, _, _) = CreateSut();
        var user = new User
        {
            Email = "u@example.com",
            EmailVerificationToken = OneTimeTokenHasher.Hash("abc"),
            EmailVerificationTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(-1),
        };
        repository.GetByTokenHashAsync(OneTimeTokenHasher.Hash("abc"), Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ConfirmByTokenAsync("abc", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }

    [Fact]
    public async Task ConfirmByTokenAsync_WhenTokenValid_ActivatesUserAndClearsToken()
    {
        var (service, repository, _, _, unitOfWork) = CreateSut();
        var user = new User
        {
            Email = "u@example.com",
            EmailVerificationToken = OneTimeTokenHasher.Hash("abc"),
            EmailVerificationTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(10),
        };
        repository.GetByTokenHashAsync(OneTimeTokenHasher.Hash("abc"), Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ConfirmByTokenAsync("abc", CancellationToken.None);

        result.Success.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.Status.Should().Be(UserStatusCatalog.Active);
        user.EmailVerificationToken.Should().BeNull();
        result.Data!.LoginUrl.Should().Be("https://app.example.com/login");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Con aprobación obligatoria, confirmar el correo lo verifica pero no activa la cuenta")]
    public async Task ConfirmByTokenAsync_WhenPendingApproval_VerifiesEmailWithoutActivating()
    {
        var (service, repository, _, emailService, unitOfWork) = CreateSut();
        var user = new User
        {
            Email = "aprobacion@example.com",
            Status = UserStatusCatalog.PendingApproval,
            EmailVerificationToken = OneTimeTokenHasher.Hash("abc"),
            EmailVerificationTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(10),
        };
        repository.GetByTokenHashAsync(OneTimeTokenHasher.Hash("abc"), Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ConfirmByTokenAsync("abc", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsActivated.Should().BeFalse();
        user.IsEmailVerified.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.Status.Should().Be(UserStatusCatalog.PendingApproval);
        user.EmailVerificationToken.Should().BeNull();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await emailService.DidNotReceive().SendAccountActivatedEmailAsync(Arg.Any<User>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Trait("Category", "Security")]
    [Theory(DisplayName = "Una cuenta rechazada o desactivada no se reactiva con un token de activación")]
    [InlineData(UserStatusCatalog.Inactive)]
    [InlineData(UserStatusCatalog.Blocked)]
    [InlineData(UserStatusCatalog.Suspended)]
    public async Task ConfirmByTokenAsync_WhenUserNotAwaitingActivation_ReturnsConflict(string status)
    {
        var (service, repository, _, _, unitOfWork) = CreateSut();
        var user = new User
        {
            Email = "rechazado@example.com",
            Status = status,
            EmailVerificationToken = OneTimeTokenHasher.Hash("abc"),
            EmailVerificationTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(10),
        };
        repository.GetByTokenHashAsync(OneTimeTokenHasher.Hash("abc"), Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ConfirmByTokenAsync("abc", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
        user.IsActive.Should().BeFalse();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Trait("Category", "Security")]
    [Theory(DisplayName = "El reenvío público no envía enlace a una cuenta rechazada ni a una que ya confirmó su correo")]
    [InlineData(UserStatusCatalog.Inactive, false)]
    [InlineData(UserStatusCatalog.PendingApproval, true)]
    public async Task ResendPublicAsync_WhenUserNotAwaitingEmailConfirmation_DoesNotSendEmail(string status, bool isEmailVerified)
    {
        var (service, repository, _, emailService, _) = CreateSut();
        var user = new User { Email = "noreenvio@example.com", Status = status, IsEmailVerified = isEmailVerified };
        repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ResendPublicAsync(user.Email, CancellationToken.None);

        result.Success.Should().BeTrue();
        await emailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    // ───────────────────────────────────────────────────────────────────────
    // ResendPublicAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResendPublicAsync_WhenEmailIsBlank_ReturnsNeutralResponse()
    {
        var (service, repository, _, emailService, _) = CreateSut();

        var result = await service.ResendPublicAsync(" ", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsActivated.Should().BeFalse();
        await repository.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await emailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task ResendPublicAsync_WhenUserNotFound_ReturnsNeutralResponse()
    {
        var (service, repository, _, _, _) = CreateSut();
        repository.GetByEmailAsync("missing@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await service.ResendPublicAsync("missing@example.com", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Email.Should().BeEmpty();
    }

    [Trait("Category", "Security")]
    [Fact(DisplayName = "ResendPublicAsync aplica un retardo artificial cuando la cuenta no existe, para no delatar por tiempo de respuesta")]
    public async Task ResendPublicAsync_WhenUserNotFound_AppliesArtificialDelay()
    {
        var (service, repository, _, _, _) = CreateSut();
        repository.GetByEmailAsync("missing@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await service.ResendPublicAsync("missing@example.com", CancellationToken.None);

        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(200, "sin el retardo artificial, una cuenta inexistente respondería casi instantáneo frente al envío SMTP real de una cuenta existente, delatando por tiempo si la cuenta existe");
    }

    [Fact]
    public async Task ResendPublicAsync_WhenUserAlreadyActive_ReturnsNeutralResponseWithoutSendingEmail()
    {
        var (service, repository, _, emailService, _) = CreateSut();
        var user = new User { Email = "active@example.com", IsActive = true };
        repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ResendPublicAsync(user.Email, CancellationToken.None);

        result.Success.Should().BeTrue();
        await emailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task ResendPublicAsync_WhenCooldownActive_ReturnsNeutralResponseWithoutSendingEmail()
    {
        var settings = new ActivationSettings { ActivationResendCooldownSeconds = 3600 };
        var (service, repository, _, emailService, _) = CreateSut(settings);
        var user = new User
        {
            Email = "cooldown@example.com",
            IsActive = false,
            EmailVerificationLastSentAt = TimeHelpers.GetCurrentMexicoTime(),
        };
        repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ResendPublicAsync(user.Email, CancellationToken.None);

        result.Success.Should().BeTrue();
        await emailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task ResendPublicAsync_WhenHourlyLimitReached_ReturnsNeutralResponseWithoutSendingEmail()
    {
        var settings = new ActivationSettings { ActivationResendMaxPerHour = 1 };
        var (service, repository, _, emailService, _) = CreateSut(settings);
        var now = TimeHelpers.GetCurrentMexicoTime();
        var user = new User
        {
            Email = "limit@example.com",
            IsActive = false,
            EmailVerificationResendWindowStartAt = now,
            EmailVerificationResendCount = 1,
        };
        repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ResendPublicAsync(user.Email, CancellationToken.None);

        result.Success.Should().BeTrue();
        await emailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task ResendPublicAsync_WhenEligible_GeneratesTokenAndSendsEmail()
    {
        var (service, repository, tokenService, emailService, unitOfWork) = CreateSut();
        var user = new User { Email = "eligible@example.com", IsActive = false };
        repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        tokenService.GenerateToken().Returns("new-token");
        emailService.SendActivationEmailAsync(user, Arg.Any<string>(), Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Ok(true));

        var result = await service.ResendPublicAsync(user.Email, CancellationToken.None);

        result.Success.Should().BeTrue();
        user.EmailVerificationToken.Should().Be(OneTimeTokenHasher.Hash("new-token"));
        await emailService.Received(1).SendActivationEmailAsync(user, "new-token", Arg.Any<CancellationToken>(), false);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResendPublicAsync_WhenEmailSendFails_ReturnsNeutralResponseAndDoesNotSave()
    {
        var (service, repository, _, emailService, unitOfWork) = CreateSut();
        var user = new User { Email = "fail@example.com", IsActive = false };
        repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        emailService.SendActivationEmailAsync(user, Arg.Any<string>(), Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "smtp down"));

        var result = await service.ResendPublicAsync(user.Email, CancellationToken.None);

        result.Success.Should().BeTrue();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ───────────────────────────────────────────────────────────────────────
    // ApproveByPublicIdAsync / RejectByPublicIdAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveByPublicIdAsync_WhenUserNotFound_ReturnsNotFound()
    {
        var (service, repository, _, _, _) = CreateSut();
        var publicId = Guid.NewGuid();
        repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await service.ApproveByPublicIdAsync(publicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApproveByPublicIdAsync_WhenUserFound_ActivatesUserWithoutVerifyingEmail(bool isEmailVerified)
    {
        var (service, repository, _, _, unitOfWork) = CreateSut();
        var user = new User { Email = "pending@example.com", PublicId = Guid.NewGuid(), IsEmailVerified = isEmailVerified };
        repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ApproveByPublicIdAsync(user.PublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.IsEmailVerified.Should().Be(isEmailVerified);
        user.Status.Should().Be(UserStatusCatalog.Active);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectByPublicIdAsync_WhenUserNotFound_ReturnsNotFound()
    {
        var (service, repository, _, _, _) = CreateSut();
        var publicId = Guid.NewGuid();
        repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await service.RejectByPublicIdAsync(publicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task RejectByPublicIdAsync_WhenUserFound_DeactivatesUser()
    {
        var (service, repository, _, _, unitOfWork) = CreateSut();
        var user = new User { Email = "pending@example.com", PublicId = Guid.NewGuid(), IsActive = true };
        repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.RejectByPublicIdAsync(user.PublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.Status.Should().Be(UserStatusCatalog.Inactive);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ───────────────────────────────────────────────────────────────────────
    // ResendAdminByPublicIdAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResendAdminByPublicIdAsync_WhenUserNotFound_ReturnsNotFound()
    {
        var (service, repository, _, _, _) = CreateSut();
        var publicId = Guid.NewGuid();
        repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await service.ResendAdminByPublicIdAsync(publicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task ResendAdminByPublicIdAsync_WhenUserAlreadyActive_ReturnsConflict()
    {
        var (service, repository, _, _, _) = CreateSut();
        var user = new User { Email = "active@example.com", PublicId = Guid.NewGuid(), IsActive = true };
        repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await service.ResendAdminByPublicIdAsync(user.PublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }

    [Fact]
    public async Task ResendAdminByPublicIdAsync_WhenEmailSendFails_ReturnsDependencyError()
    {
        var (service, repository, _, emailService, unitOfWork) = CreateSut();
        var user = new User { Email = "pending@example.com", PublicId = Guid.NewGuid(), IsActive = false };
        repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
        emailService.SendActivationEmailAsync(user, Arg.Any<string>(), Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "smtp down"));

        var result = await service.ResendAdminByPublicIdAsync(user.PublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResendAdminByPublicIdAsync_WhenEligible_SendsEmailAndSaves()
    {
        var (service, repository, tokenService, emailService, unitOfWork) = CreateSut();
        var user = new User { Email = "pending@example.com", PublicId = Guid.NewGuid(), IsActive = false };
        repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
        tokenService.GenerateToken().Returns("admin-token");
        emailService.SendActivationEmailAsync(user, Arg.Any<string>(), Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Ok(true));

        var result = await service.ResendAdminByPublicIdAsync(user.PublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        user.EmailVerificationToken.Should().Be(OneTimeTokenHasher.Hash("admin-token"));
        await emailService.Received(1).SendActivationEmailAsync(user, "admin-token", Arg.Any<CancellationToken>(), false);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "Aprobar, rechazar o reenviar la activacion de una cuenta con flags requiere nivel igual o superior")]
    [InlineData("approve")]
    [InlineData("reject")]
    [InlineData("resend")]
    public async Task AdminOperations_OnPrivilegedTargetWithoutLevel_ReturnForbidden(string operation)
    {
        var (service, repository, _, emailService, unitOfWork) = CreateSut();
        var user = new User { Email = "su@example.com", PublicId = Guid.NewGuid(), IsActive = true, IsSuAdmin = true };
        repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

        var result = operation switch
        {
            "approve" => await service.ApproveByPublicIdAsync(user.PublicId, CancellationToken.None),
            "reject" => await service.RejectByPublicIdAsync(user.PublicId, CancellationToken.None),
            _ => await service.ResendAdminByPublicIdAsync(user.PublicId, CancellationToken.None),
        };

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status403Forbidden);
        user.IsActive.Should().BeTrue("un administrador por rol no puede desactivar a un IsSuAdmin");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await emailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    // ───────────────────────────────────────────────────────────────────────
    // SendTestEmailAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendTestEmailAsync_DelegatesToEmailService()
    {
        var (service, _, _, emailService, _) = CreateSut();
        emailService.SendTestEmailAsync("subject", "body", Arg.Any<CancellationToken>(), true)
          .Returns(OperationResult.Ok(true));

        var result = await service.SendTestEmailAsync("subject", "body", CancellationToken.None, true);

        result.Success.Should().BeTrue();
        await emailService.Received(1).SendTestEmailAsync("subject", "body", Arg.Any<CancellationToken>(), true);
    }
}

// -----------------------------------------------------------------------
// <copyright file="ExternalLoginOrchestrator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;

public class ExternalLoginOrchestrator(
  ExternalLoginProviderResolver providerResolver,
  ExternalLoginSessionService sessionService,
  AutoDiscoveryIAMDbContext dbContext,
  IPasswordHasher passwordHasher,
    IAuthLogger logger,
    IActivationService activationService,
    IActivationEmailService activationEmailService,
    ITwoFactorCodeGenerator twoFactorCodeGenerator,
    ITwoFactorEmailService twoFactorEmailService,
    IOptions<TwoFactorSettings> twoFactorSettings)
{
    private readonly ExternalLoginProviderResolver _providerResolver = providerResolver;
    private readonly ExternalLoginSessionService _sessionService = sessionService;
    private readonly AutoDiscoveryIAMDbContext _dbContext = dbContext;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IAuthLogger _logger = logger;
    private readonly IActivationService _activationService = activationService;
    private readonly IActivationEmailService _activationEmailService = activationEmailService;
    private readonly ITwoFactorCodeGenerator _twoFactorCodeGenerator = twoFactorCodeGenerator;
    private readonly ITwoFactorEmailService _twoFactorEmailService = twoFactorEmailService;
    private readonly TwoFactorSettings _twoFactorSettings = twoFactorSettings.Value;

    public async Task<OperationResult<LoginResponseDto>> HandleCallbackAsync(
      string providerName,
      string code,
      string? ipAddress,
      string? userAgent,
      CancellationToken cancellationToken)
    {
        var externalIdentityResult = await ExchangeExternalIdentityAsync(providerName, code, cancellationToken);
        if (!externalIdentityResult.Success || externalIdentityResult.Data == null)
        {
            return OperationResult.Fail<LoginResponseDto>(externalIdentityResult.ErrorInfo!, externalIdentityResult.Error!);
        }

        var userResult = await ResolveUserFromExternalIdentityAsync(externalIdentityResult.Data, cancellationToken);
        if (!userResult.Success)
        {
            return OperationResult.Fail<LoginResponseDto>(userResult.ErrorInfo!, userResult.Error!);
        }

        var user = userResult.Data!;
        if (!user.IsActive)
        {
            _logger.LogLoginFailed(user.Email);
            return OperationResult.Fail<LoginResponseDto>(
                ErrorInfo.AccountPendingOrInactive,
                "Tu cuenta esta pendiente de activacion o inactiva. Revisa tu correo o solicita reenvio de activacion.");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > TimeHelpers.GetCurrentMexicoTime())
        {
            _logger.LogUserBlocked(user.Email);
            return OperationResult.Fail<LoginResponseDto>(
                ErrorInfo.AccountLockedOut,
                "Cuenta bloqueada temporalmente por multiples intentos fallidos. Intenta de nuevo mas tarde o restablece tu contrasena.");
        }

        // Google prueba la identidad, pero no reemplaza el segundo factor que el usuario activó: se
        // responde igual que el login con contraseña, con el reto en vez de tokens.
        if (user.TwoFactorEnabled)
        {
            var challenge = await TwoFactorChallengeStarter.BeginAsync(user, _twoFactorCodeGenerator, _twoFactorEmailService, _twoFactorSettings, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return OperationResult.Ok(challenge);
        }

        var session = await _sessionService.CreateSessionAsync(user, ipAddress, userAgent, cancellationToken);
        _logger.LogLoginSuccess(user.Email);
        _logger.LogTokenGenerated(user.Email);
        return OperationResult.Ok(session);
    }

    public async Task<OperationResult<ExternalLinkOperationResult>> LinkExternalIdentityAsync(
      string providerName,
      string code,
      Guid currentUserPublicId,
      CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(currentUserPublicId, cancellationToken);
        if (currentUser == null)
        {
            return OperationResult.Fail<ExternalLinkOperationResult>(ErrorInfo.Status401Unauthorized, "No se pudo resolver el usuario autenticado.");
        }

        var identityResult = await ExchangeExternalIdentityAsync(providerName, code, cancellationToken);
        if (!identityResult.Success || identityResult.Data == null)
        {
            return OperationResult.Fail<ExternalLinkOperationResult>(identityResult.ErrorInfo!, identityResult.Error!);
        }

        var conflictResult = await ValidateLinkConflictsAsync(currentUser, identityResult.Data, cancellationToken);
        if (conflictResult != null)
        {
            return conflictResult;
        }

        await PersistExternalLinkAsync(currentUser, identityResult.Data, cancellationToken);
        return OperationResult.Ok(new ExternalLinkOperationResult("Cuenta de Google vinculada correctamente."));
    }

    public async Task<OperationResult<ExternalLinkOperationResult>> UnlinkExternalIdentityAsync(
      string providerName,
      Guid currentUserPublicId,
      CancellationToken cancellationToken)
    {
        var currentUser = await _dbContext.Users
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.PublicId == currentUserPublicId, cancellationToken);

        if (currentUser == null)
        {
            return OperationResult.Fail<ExternalLinkOperationResult>(ErrorInfo.Status401Unauthorized, "No se pudo resolver el usuario autenticado.");
        }

        var link = await _dbContext.ExternalLogins
          .FirstOrDefaultAsync(
            x => !x.IsDeleted && x.UserId == currentUser.Id && x.Provider == providerName,
            cancellationToken);

        if (link == null)
        {
            return OperationResult.Fail<ExternalLinkOperationResult>(
              ErrorInfo.Status404NotFound,
              "No existe una vinculacion activa de Google para este usuario.");
        }

        _dbContext.ExternalLogins.Remove(link);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return OperationResult.Ok(new ExternalLinkOperationResult("Vinculacion de Google eliminada correctamente."));
    }

    private async Task<OperationResult<ExternalIdentityProfile>> ExchangeExternalIdentityAsync(string providerName, string code, CancellationToken cancellationToken)
    {
        var provider = _providerResolver.Resolve(providerName);
        if (provider == null)
        {
            return OperationResult.Fail<ExternalIdentityProfile>(ErrorInfo.Status400Validation, $"Proveedor externo no soportado: {providerName}.");
        }

        var exchangeResult = await provider.ExchangeCodeAsync(code, cancellationToken);
        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return OperationResult.Fail<ExternalIdentityProfile>(
              exchangeResult.ErrorInfo ?? ErrorInfo.Status424Dependency,
              exchangeResult.Error ?? "No fue posible validar la identidad externa.");
        }

        if (!exchangeResult.Data.IsEmailVerified)
        {
            return OperationResult.Fail<ExternalIdentityProfile>(ErrorInfo.Status401Unauthorized, "La cuenta de Google debe tener correo verificado.");
        }

        // se hashea el identificador de sujeto del proveedor (sub) justo aqui, una sola
        // vez, antes de que cualquier otro metodo lo compare o persista — todo el resto de este
        // orquestador solo ve/guarda el hash, nunca el valor en claro.
        var hashedIdentity = exchangeResult.Data with { ProviderId = ExternalProviderIdHasher.Hash(exchangeResult.Data.ProviderId) };
        return OperationResult.Ok(hashedIdentity);
    }

    private async Task<OperationResult<User>> ResolveUserFromExternalIdentityAsync(ExternalIdentityProfile externalIdentity, CancellationToken cancellationToken)
    {
        var externalLogin = await _dbContext.ExternalLogins
          .Include(x => x.User)
          .ThenInclude(u => u.UserRoles)
          .ThenInclude(ur => ur.Role)
          .FirstOrDefaultAsync(
            x => !x.IsDeleted && x.Provider == externalIdentity.Provider && x.ProviderId == externalIdentity.ProviderId,
            cancellationToken);

        if (externalLogin != null)
        {
            return OperationResult.Ok(externalLogin.User);
        }

        var (user, wasCreated) = await ResolveOrCreateUserAsync(externalIdentity, cancellationToken);

        // Vincular por coincidencia de email le daría la cuenta a quien controle una cuenta de Google
        // con ese correo (por ejemplo, el administrador de su dominio de Google Workspace). En cuentas
        // con flags especiales eso sería una escalada: deben vincular Google con su sesión iniciada.
        if (!wasCreated && (user.IsAdmin || user.IsDevelopment || user.IsSuAdmin))
        {
            _logger.LogLoginFailed(user.Email);
            return OperationResult.Fail<User>(
              ErrorInfo.Status409Conflict,
              "Esta cuenta no se vincula a Google automaticamente. Inicia sesion con tu contrasena y vinculala desde POST /auth/google/link.");
        }

        if (wasCreated)
        {
            await InitializeActivationForNewSocialUserAsync(user, cancellationToken);
        }
        else if (!user.IsEmailVerified)
        {
            InvalidateUnverifiedPassword(user);
        }

        await CreateExternalLinkAsync(user, externalIdentity, cancellationToken);

        var resolved = await _dbContext.Users
          .Include(u => u.UserRoles)
          .ThenInclude(ur => ur.Role)
          .FirstAsync(u => u.Id == user.Id, cancellationToken);
        return OperationResult.Ok(resolved);
    }

    /// <summary>
    /// Cuenta existente cuyo email nunca se verificó: pudo registrarla cualquiera con el correo de la
    /// víctima y una contraseña propia, esperando a que la víctima entre con Google. Google acaba de
    /// probar quién es el dueño del correo, así que la contraseña anterior deja de servir (el dueño
    /// define una nueva con el restablecimiento) y se invalidan los tokens ya emitidos.
    /// </summary>
    private void InvalidateUnverifiedPassword(User user)
    {
        user.PasswordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));
        user.TokenValidFrom = TimeHelpers.GetCurrentMexicoTime();
    }

    private async Task CreateExternalLinkAsync(User user, ExternalIdentityProfile externalIdentity, CancellationToken cancellationToken)
    {
        var newExternalLogin = new ExternalLogin
        {
            UserId = user.Id,
            Provider = externalIdentity.Provider,
            ProviderId = externalIdentity.ProviderId,
        };

        _dbContext.ExternalLogins.Add(newExternalLogin);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<User?> ResolveCurrentUserAsync(Guid currentUserPublicId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.PublicId == currentUserPublicId, cancellationToken);
    }

    private async Task<OperationResult<ExternalLinkOperationResult>?> ValidateLinkConflictsAsync(User currentUser, ExternalIdentityProfile identity, CancellationToken cancellationToken)
    {
        var providerIdentityAlreadyLinked = await _dbContext.ExternalLogins
          .FirstOrDefaultAsync(
            x => x.Provider == identity.Provider && x.ProviderId == identity.ProviderId,
            cancellationToken);

        if (providerIdentityAlreadyLinked != null)
        {
            if (providerIdentityAlreadyLinked.IsDeleted)
            {
                return null;
            }

            if (providerIdentityAlreadyLinked.UserId == currentUser.Id)
            {
                return OperationResult.Ok(new ExternalLinkOperationResult("La cuenta de Google ya estaba vinculada a tu usuario."));
            }

            return OperationResult.Fail<ExternalLinkOperationResult>(
              ErrorInfo.Status409Conflict,
              "Esta cuenta de Google ya esta vinculada a otro usuario del sistema.");
        }

        var userProviderLink = await _dbContext.ExternalLogins
          .FirstOrDefaultAsync(
            x => x.UserId == currentUser.Id && x.Provider == identity.Provider,
            cancellationToken);

        if (userProviderLink != null)
        {
            if (userProviderLink.IsDeleted)
            {
                return null;
            }

            return OperationResult.Fail<ExternalLinkOperationResult>(
              ErrorInfo.Status409Conflict,
              "Tu usuario ya tiene una cuenta Google vinculada.");
        }

        return null;
    }

    private async Task PersistExternalLinkAsync(User currentUser, ExternalIdentityProfile identity, CancellationToken cancellationToken)
    {
        var reusable = await FindReusableLinkAsync(currentUser.Id, identity.Provider, identity.ProviderId, cancellationToken);

        if (reusable == null)
        {
            _dbContext.ExternalLogins.Add(new ExternalLogin
            {
                UserId = currentUser.Id,
                Provider = identity.Provider,
                ProviderId = identity.ProviderId,
            });
        }
        else
        {
            reusable.UserId = currentUser.Id;
            reusable.Provider = identity.Provider;
            reusable.ProviderId = identity.ProviderId;
            reusable.IsDeleted = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ExternalLogin?> FindReusableLinkAsync(long userId, string provider, string providerId, CancellationToken cancellationToken)
    {
        var byProviderIdentity = await _dbContext.ExternalLogins
          .FirstOrDefaultAsync(
            x => x.Provider == provider && x.ProviderId == providerId,
            cancellationToken);

        if (byProviderIdentity != null)
        {
            return byProviderIdentity;
        }

        return await _dbContext.ExternalLogins
          .FirstOrDefaultAsync(
            x => x.UserId == userId && x.Provider == provider,
            cancellationToken);
    }

    private async Task<(User User, bool WasCreated)> ResolveOrCreateUserAsync(ExternalIdentityProfile profile, CancellationToken cancellationToken)
    {
        var existingUser = await _dbContext.Users
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == profile.Email, cancellationToken);

        if (existingUser != null)
        {
            return (existingUser, false);
        }

        var username = await BuildUniqueUsernameAsync(profile.Email, cancellationToken);

        var user = new User
        {
            Username = username,
            Email = profile.Email,
            PasswordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString("N")),
            Status = UserStatusCatalog.Pending,
            IsActive = false,
            IsEmailVerified = false,
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (user, true);
    }

    private async Task InitializeActivationForNewSocialUserAsync(User user, CancellationToken cancellationToken)
    {
        var activationToken = _activationService.AssignActivationToken(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(activationToken))
        {
            return;
        }

        await _activationEmailService.SendActivationEmailAsync(
            user,
            activationToken,
            cancellationToken,
            fakeMail: false);
    }

    private async Task<string> BuildUniqueUsernameAsync(string email, CancellationToken cancellationToken)
    {
        var localPart = email.Split('@')[0];
        var sanitizedBase = string.Concat(localPart.Where(char.IsLetterOrDigit));
        if (string.IsNullOrWhiteSpace(sanitizedBase))
        {
            sanitizedBase = "user";
        }

        var baseUsername = sanitizedBase.Length > 70 ? sanitizedBase[..70] : sanitizedBase;
        var candidate = baseUsername;
        var suffix = 1;

        while (await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == candidate, cancellationToken))
        {
            candidate = $"{baseUsername}_{suffix}";
            if (candidate.Length > 100)
            {
                candidate = candidate[..100];
            }

            suffix++;
        }

        return candidate;
    }
}

public record ExternalLinkOperationResult(string Message);

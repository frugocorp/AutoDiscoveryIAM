// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.DefaultRole;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Alta de un usuario por un administrador. A diferencia del registro público, el correo duplicado
/// sí se informa (409): quien llama ya tiene el permiso de administrar usuarios, así que no hay
/// enumeración que proteger, y una respuesta neutra le daría un identificador que no existe.
/// </summary>
public class CreateUserByAdminHandler(
  ICreateUserByAdminRepository repository,
  IUserLogger logger,
  IAutoDiscoveryIAMUnitOfWork unitOfWork,
  IPasswordHasher passwordHasher,
  IActivationService activationService,
  IActivationEmailService activationEmailService,
  IDefaultRoleAssignmentService defaultRoleAssignmentService,
  IPasswordResetTokenService passwordResetTokenService,
  IPasswordResetEmailService passwordResetEmailService,
  IOptions<RegistrationSettings> registrationSettings)
{
  private readonly ICreateUserByAdminRepository _repository = repository;
  private readonly IUserLogger _logger = logger;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;
  private readonly IPasswordHasher _passwordHasher = passwordHasher;
  private readonly IActivationService _activationService = activationService;
  private readonly IActivationEmailService _activationEmailService = activationEmailService;
  private readonly IDefaultRoleAssignmentService _defaultRoleAssignmentService = defaultRoleAssignmentService;
  private readonly IPasswordResetTokenService _passwordResetTokenService = passwordResetTokenService;
  private readonly IPasswordResetEmailService _passwordResetEmailService = passwordResetEmailService;
  private readonly RegistrationSettings _registrationSettings = registrationSettings.Value;

  /// <summary>
  /// Crea el usuario y le asigna el rol por defecto. Según la petición, la cuenta nace activa, pendiente
  /// de activación por correo, o activa con un correo para definir la contraseña (sin contraseña inicial).
  /// </summary>
  /// <param name="command">Datos del alta.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Datos del usuario creado o el error.</returns>
  public Task<OperationResult<CreateUserByAdminResponseDto>> Handle(CreateUserByAdminCommand command, CancellationToken cancellationToken)
    => HandleAsync(command, beforeSave: null, cancellationToken);

  /// <summary>
  /// Igual que <see cref="Handle"/>, y antes de enviar el correo y guardar llama a
  /// <paramref name="beforeSave"/> con el usuario nuevo, para que el consumidor agregue su entidad
  /// ligada en el mismo guardado (ver <c>IUserProvisioningService</c>). Si el alta no termina, el
  /// usuario sale del contexto: un <c>SaveChangesAsync</c> posterior del consumidor no lo guarda.
  /// </summary>
  /// <param name="command">Datos del alta.</param>
  /// <param name="beforeSave">Enlace opcional de la entidad propia del consumidor.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Datos del usuario creado o el error.</returns>
  internal async Task<OperationResult<CreateUserByAdminResponseDto>> HandleAsync(CreateUserByAdminCommand command, Func<User, CancellationToken, Task>? beforeSave, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(command);

    if (await _repository.ExistsByEmailAsync(command.Email, cancellationToken))
    {
      return OperationResult.Fail<CreateUserByAdminResponseDto>(ErrorInfo.Status409Conflict, UserValidationTypes.EmailExists.Message);
    }

    if (await _repository.ExistsByUsernameAsync(command.Username, cancellationToken))
    {
      return OperationResult.Fail<CreateUserByAdminResponseDto>(ErrorInfo.Status409Conflict, UserValidationTypes.UsernameExists.Message);
    }

    var isInvitation = command.Password == null;
    var user = BuildUser(command, isInvitation);
    _repository.Add(user);
    try
    {
      await _defaultRoleAssignmentService.AssignDefaultRoleAsync(user, cancellationToken);
      if (beforeSave != null)
      {
        await RunBeforeSaveAsync(user, beforeSave, cancellationToken);
      }

      var sendResult = await SendInitialEmailAsync(user, command, isInvitation, cancellationToken);
      if (sendResult is { Success: false })
      {
        _repository.Discard(user);
        return OperationResult.Fail<CreateUserByAdminResponseDto>(
          sendResult.ErrorInfo ?? ErrorInfo.Status424Dependency,
          sendResult.Error ?? "No fue posible enviar el correo al usuario.");
      }

      await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch
    {
      _repository.Discard(user);
      throw;
    }

    _logger.LogUserCreated(user.Id, user.Username, user.Email);
    return OperationResult.Ok(new CreateUserByAdminResponseDto(
      user.PublicId,
      user.Email,
      user.Username,
      user.Status,
      ActivationEmailSent: !isInvitation && !command.Activate,
      PasswordSetupEmailSent: isInvitation,
      MustChangePassword: user.MustChangePassword));
  }

  /// <summary>
  /// El enlace del consumidor corre antes del correo, así un fallo suyo no deja una invitación
  /// enviada para una cuenta que no existe. Solo puede enlazar: si cambia lo que fija la regla de alta,
  /// el alta se cancela.
  /// </summary>
  private static async Task RunBeforeSaveAsync(User user, Func<User, CancellationToken, Task> beforeSave, CancellationToken cancellationToken)
  {
    var expected = ProvisioningSnapshot.Of(user);
    await beforeSave(user, cancellationToken);
    if (ProvisioningSnapshot.Of(user) != expected)
    {
      throw new InvalidOperationException(
        "El enlace previo al guardado cambió datos del usuario que fija la regla de alta (correo, usuario, contraseña, estado, verificación o banderas de administración). Solo debe agregar la entidad propia ligada al usuario.");
    }
  }

  private User BuildUser(CreateUserByAdminCommand command, bool isInvitation)
  {
    var user = new User
    {
      Username = command.Username,
      Email = command.Email,
      // Invitación: una contraseña aleatoria que nadie conoce, hasta que el usuario defina la suya.
      PasswordHash = _passwordHasher.HashPassword(command.Password ?? Guid.NewGuid().ToString("N")),
      // La contraseña que eligió el administrador también la conoce él: por defecto el usuario la
      // cambia al entrar, salvo que el administrador pida lo contrario de forma explícita.
      MustChangePassword = !isInvitation && (command.MustChangePassword ?? true),
    };
    if (command.Activate || isInvitation)
    {
      // El administrador responde por la cuenta, pero no prueba de quién es el correo: IsEmailVerified
      // queda en false hasta que el usuario lo confirme (activación, enlace para definir la contraseña o
      // restablecimiento). Así, el primer inicio de sesión del dueño con Google invalida la contraseña
      // que definió el administrador.
      user.IsActive = true;
      user.Status = UserStatusCatalog.Active;
    }

    return user;
  }

  /// <summary>
  /// Un solo correo según el caso: invitación para definir la contraseña, activación, o ninguno.
  /// </summary>
  private async Task<OperationResult<bool>?> SendInitialEmailAsync(User user, CreateUserByAdminCommand command, bool isInvitation, CancellationToken cancellationToken)
  {
    if (isInvitation)
    {
      // Mismo mecanismo que el restablecimiento (solo el hash queda en la base), con una vigencia
      // pensada para una invitación en vez de minutos.
      var setupToken = _passwordResetTokenService.GenerateToken();
      user.PasswordResetToken = OneTimeTokenHasher.Hash(setupToken);
      user.PasswordResetTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddHours(Math.Max(_registrationSettings.PasswordSetupLinkExpiryHours, 1));
      return await _passwordResetEmailService.SendPasswordSetupEmailAsync(user, setupToken, cancellationToken, fakeMail: false);
    }

    if (!command.Activate)
    {
      return await _activationEmailService.SendActivationEmailAsync(user, _activationService.AssignActivationToken(user), cancellationToken, fakeMail: false);
    }

    return null;
  }

  /// <summary>
  /// Lo que fija la regla de alta y el enlace del consumidor no puede cambiar.
  /// </summary>
  private sealed record ProvisioningSnapshot(
    string Username,
    string Email,
    string PasswordHash,
    string Status,
    bool IsActive,
    bool IsEmailVerified,
    bool MustChangePassword,
    bool IsAdmin,
    bool IsDevelopment,
    bool IsSuAdmin,
    string? PasswordResetToken)
  {
    public static ProvisioningSnapshot Of(User user) => new(
      user.Username,
      user.Email,
      user.PasswordHash,
      user.Status,
      user.IsActive,
      user.IsEmailVerified,
      user.MustChangePassword,
      user.IsAdmin,
      user.IsDevelopment,
      user.IsSuAdmin,
      user.PasswordResetToken);
  }
}

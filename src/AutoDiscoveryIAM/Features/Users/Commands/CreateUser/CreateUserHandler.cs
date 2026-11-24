// -----------------------------------------------------------------------
// <copyright file="CreateUserHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.DefaultRole;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;

public class CreateUserHandler(
    ICreateUserRepository repository,
    IUserLogger logger,
    IAutoDiscoveryIAMUnitOfWork unitOfWork,
  IPasswordHasher passwordHasher,
  IActivationService activationService,
  IActivationEmailService activationEmailService,
  IDefaultRoleAssignmentService defaultRoleAssignmentService,
  IOptions<RegistrationSettings> registrationSettings)
{
  private readonly ICreateUserRepository _repository = repository;
  private readonly IUserLogger _logger = logger;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;
  private readonly IPasswordHasher _passwordHasher = passwordHasher;
  private readonly IActivationService _activationService = activationService;
  private readonly IActivationEmailService _activationEmailService = activationEmailService;
  private readonly IDefaultRoleAssignmentService _defaultRoleAssignmentService = defaultRoleAssignmentService;
  private readonly RegistrationSettings _registrationSettings = registrationSettings.Value;

  public async Task<OperationResult<CreateUserResponseDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
  {
    var user = new User
    {
      Username = command.Username,
      Email = command.Email,
      PasswordHash = _passwordHasher.HashPassword(command.Password),
      Status = _registrationSettings.SelfRegistrationStatus,
    };
    var exists = await _repository.ExistsByEmailAsync(command.Email, cancellationToken);
    if (exists)
    {
      return await RespondNeutrallyToExistingEmailAsync(command, cancellationToken);
    }

    if (await _repository.ExistsByUsernameAsync(command.Username, cancellationToken))
    {
      return OperationResult.Fail<CreateUserResponseDto>(ErrorInfo.Status409Conflict, UserValidationTypes.UsernameExists.Message);
    }

    await _repository.CreateUser(user);
    await _defaultRoleAssignmentService.AssignDefaultRoleAsync(user, cancellationToken);

    var activationToken = _activationService.AssignActivationToken(user);

    var sendResult = await _activationEmailService.SendActivationEmailAsync(
      user,
      activationToken,
      cancellationToken,
      fakeMail: false);
    if (!sendResult.Success)
    {
      return OperationResult.Fail<CreateUserResponseDto>(
        sendResult.ErrorInfo ?? ErrorInfo.Status424Dependency,
        sendResult.Error ?? "No fue posible enviar correo de activacion.");
    }

    await _unitOfWork.SaveChangesAsync(cancellationToken);
    _logger.LogUserCreated(user.Id, user.Username, user.Email);
    var responseDto = new CreateUserResponseDto(user.PublicId, user.Email, user.Username, user.Status);
    return OperationResult.Ok(responseDto);
  }

  /// <summary>
  /// Registro con un correo ya registrado: misma respuesta que un registro nuevo (con un PublicId que
  /// no corresponde a ninguna cuenta) y el mismo trabajo (hash de la contraseña y un envío de correo),
  /// para que ni el cuerpo ni el tiempo de respuesta delaten qué correos existen. El dueño recibe un
  /// aviso en lugar del enlace de activación.
  /// </summary>
  private async Task<OperationResult<CreateUserResponseDto>> RespondNeutrallyToExistingEmailAsync(CreateUserCommand command, CancellationToken cancellationToken)
  {
    // Best-effort: si el aviso falla, la respuesta sigue siendo la neutra (un 424 aquí delataría
    // que el correo existe).
    await _activationEmailService.SendRegistrationAttemptNoticeAsync(command.Email, cancellationToken, fakeMail: false);

    return OperationResult.Ok(new CreateUserResponseDto(Guid.NewGuid(), command.Email, command.Username, _registrationSettings.SelfRegistrationStatus));
  }
}

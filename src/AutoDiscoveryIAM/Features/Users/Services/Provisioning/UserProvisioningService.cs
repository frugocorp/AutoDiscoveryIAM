// -----------------------------------------------------------------------
// <copyright file="UserProvisioningService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.Provisioning;

/// <inheritdoc />
/// <remarks>
/// Es el único camino del alta por administrador: el propio endpoint <c>POST /users/by-admin</c> lo
/// usa, así que la validación nunca queda fuera aunque el alta venga del código del consumidor.
/// </remarks>
public class UserProvisioningService(
  CreateUserByAdminCommandValidator validator,
  CreateUserByAdminHandler handler) : IUserProvisioningService
{
  private readonly CreateUserByAdminCommandValidator _validator = validator;
  private readonly CreateUserByAdminHandler _handler = handler;

  /// <inheritdoc />
  public Task<OperationResult<ProvisionedUser>> CreateAsync(ProvisionUserRequest request, CancellationToken cancellationToken)
    => CreateCoreAsync(request, beforeSave: null, cancellationToken);

  /// <inheritdoc />
  public Task<OperationResult<ProvisionedUser>> CreateAsync(ProvisionUserRequest request, Func<User, CancellationToken, Task> beforeSave, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(beforeSave);
    return CreateCoreAsync(request, beforeSave, cancellationToken);
  }

  private async Task<OperationResult<ProvisionedUser>> CreateCoreAsync(ProvisionUserRequest request, Func<User, CancellationToken, Task>? beforeSave, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

    var command = new CreateUserByAdminCommand(request.Username, request.Email, request.Password, request.Activate, request.MustChangePassword);
    var validation = await _validator.ValidateAsync(command, cancellationToken);
    if (!validation.IsValid)
    {
      return OperationResult.Fail<ProvisionedUser>(
        ErrorInfo.Status400Validation,
        string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
    }

    var result = await _handler.HandleAsync(command, beforeSave, cancellationToken);
    if (!result.Success)
    {
      return OperationResult.Fail<ProvisionedUser>(
        result.ErrorInfo ?? ErrorInfo.Status500Unknown,
        result.Error ?? "No fue posible crear el usuario.");
    }

    var data = result.Data!;
    return OperationResult.Ok(new ProvisionedUser(
      data.PublicId,
      data.Email,
      data.Username,
      data.Status,
      data.ActivationEmailSent,
      data.PasswordSetupEmailSent,
      data.MustChangePassword));
  }
}

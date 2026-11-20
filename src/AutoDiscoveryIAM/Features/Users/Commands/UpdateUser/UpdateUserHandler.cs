// -----------------------------------------------------------------------
// <copyright file="UpdateUserHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

/// <summary>
/// Handler que procesa el comando de actualización general de usuario.
/// </summary>
/// <remarks>
/// Maneja la lógica de negocio para actualizar información básica del usuario
/// como username, email y notas. Incluye validaciones de unicidad.
/// </remarks>
public class UpdateUserHandler(
    IUpdateUserRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork,
    IUserLogger logger,
    IAuthContextService authContextService)
{

  /// <summary>
  /// Maneja el comando de actualización de usuario.
  /// </summary>
  /// <param name="command">Comando con los datos de actualización.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Resultado de la operación con el usuario actualizado.</returns>
  public async Task<OperationResult<UpdateUserResponseDto>> Handle(
      UpdateUserCommand command,
      CancellationToken cancellationToken = default)
  {
    // Buscar usuario
    var user = await repository.GetByPublicIdAsync(command.PublicId, cancellationToken);
    if (user is null)
    {
      logger.LogUserNotFoundByPublicId(command.PublicId);
      return OperationResult.Fail<UpdateUserResponseDto>(
          ErrorInfo.Status404NotFound,
          $"Usuario con PublicId {command.PublicId} no encontrado");
    }

    // Cambiar el email de una cuenta con flags permitiría tomarla con el reseteo público de
    // contraseña: solo la edita quien tiene un nivel igual o superior (ver UserAccessHierarchy).
    if (!await UserAccessHierarchy.CanManageAsync(authContextService, user))
    {
      return OperationResult.Fail<UpdateUserResponseDto>(ErrorInfo.Status403Forbidden, UserAccessHierarchy.InsufficientLevelMessage);
    }

    return await ProcessUserUpdatesAsync(command, user, cancellationToken);
  }

  private async Task<OperationResult<UpdateUserResponseDto>> ProcessUserUpdatesAsync(
      UpdateUserCommand command,
      Features.Users.Models.Entities.User user,
      CancellationToken cancellationToken)
  {
    var hasChanges = false;

    // Actualizar username si se proporciona
    if (!string.IsNullOrWhiteSpace(command.Request.Username) &&
        command.Request.Username != user.Username)
    {
      // Verificar unicidad del username
      var existsUsername = await repository.ExistsUsernameAsync(
          command.Request.Username, user.Id, cancellationToken);

      if (existsUsername)
      {
        return OperationResult.Fail<UpdateUserResponseDto>(
            ErrorInfo.Status409Conflict,
            $"Ya existe otro usuario con el username '{command.Request.Username}'");
      }

      user.Username = command.Request.Username;
      hasChanges = true;
    }

    // Actualizar email si se proporciona
    if (!string.IsNullOrWhiteSpace(command.Request.Email) &&
        command.Request.Email != user.Email)
    {
      // Verificar unicidad del email
      var existsEmail = await repository.ExistsEmailAsync(
          command.Request.Email, user.Id, cancellationToken);

      if (existsEmail)
      {
        return OperationResult.Fail<UpdateUserResponseDto>(
            ErrorInfo.Status409Conflict,
            $"Ya existe otro usuario con el email '{command.Request.Email}'");
      }

      user.Email = command.Request.Email;
      hasChanges = true;
    }

    return await FinalizeUserUpdateAsync(command, user, hasChanges, cancellationToken);
  }

  private async Task<OperationResult<UpdateUserResponseDto>> FinalizeUserUpdateAsync(
      UpdateUserCommand command,
      Features.Users.Models.Entities.User user,
      bool hasChanges,
      CancellationToken cancellationToken)
  {
    // Si no hay cambios, devolver respuesta sin modificar base de datos
    if (!hasChanges)
    {
      var unchangedResponse = new UpdateUserResponseDto(
          user.PublicId,
          user.Username,
          user.Email,
          user.UpdatedAt ?? DateTime.UtcNow,
          command.Request.Notes ?? "Sin cambios detectados");

      return OperationResult.Ok(unchangedResponse);
    }

    // Actualizar timestamp
    user.UpdatedAt = DateTime.UtcNow;

    // Guardar cambios
    try
    {
      await unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      return OperationResult.Fail<UpdateUserResponseDto>(
          ErrorInfo.Status409Conflict,
          "El usuario fue modificado por otro proceso mientras tanto. Recarga los datos e intenta de nuevo.");
    }

    logger.LogUserUpdatedSuccessfully(user.PublicId, $"Usuario actualizado: {user.Username}, {user.Email}");

    var response = new UpdateUserResponseDto(
        user.PublicId,
        user.Username,
        user.Email,
        user.UpdatedAt ?? DateTime.UtcNow,
        command.Request.Notes ?? "Usuario actualizado correctamente");

    return OperationResult.Ok(response);
  }
}

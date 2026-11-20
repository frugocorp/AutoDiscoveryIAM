// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

/// <summary>
/// Handler para cambiar el tipo de usuario a Admin a partir de un comando <see cref="UpdateUserTypeAdminCommand"/>.
/// </summary>
/// <param name="repository">Repositorio de acceso a datos de usuarios (<see cref="IUpdateUserTypeAdminRepository"/>).</param>
/// <param name="logger">Instancia de <see cref="IUserLogger"/> para registrar eventos y errores relacionados con usuarios.</param>
/// <param name="unitOfWork">Unidad de trabajo para gestionar la transacción.</param>
/// <param name="authContextService">Usuario de la petición, para la jerarquía de niveles (<see cref="Infrastructure.Authorization.UserAccessHierarchy"/>).</param>
/// <remarks>
/// Valida la existencia del usuario, aplica reglas de negocio críticas y persiste los cambios.
/// Esta operación es crítica y solo puede ser ejecutada por usuarios SuAdmin o Development.
/// Utiliza <see cref="IUpdateUserTypeAdminRepository"/> para acceso a datos y <see cref="IUserLogger"/> para logging.
/// </remarks>
public class UpdateUserTypeAdminHandler(IUpdateUserTypeAdminRepository repository, IUserLogger logger, IAutoDiscoveryIAMUnitOfWork unitOfWork, IAuthContextService authContextService)
{
  private readonly IAuthContextService _authContextService = authContextService;
  private readonly IUpdateUserTypeAdminRepository _repository = repository;
  private readonly IUserLogger _logger = logger;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

  /// <summary>
  /// Maneja el cambio de tipo de usuario a Admin.
  /// </summary>
  /// <param name="command">Comando con el PublicId y los datos del cambio de tipo.</param>
  /// <param name="cancellationToken">Token para cancelar la operación.</param>
  /// <returns>Resultado de la operación y el usuario actualizado en caso de éxito.</returns>
  public async Task<OperationResult<UpdateUserTypeAdminResponseDto>> Handle(UpdateUserTypeAdminCommand command, CancellationToken cancellationToken)
  {
    var user = await _repository.GetByPublicIdAsync(command.PublicId, cancellationToken);
    if (user == null)
    {
      _logger.LogUserNotFoundByPublicId(command.PublicId);
      return OperationResult.Fail<UpdateUserTypeAdminResponseDto>(
          ErrorInfo.Status404NotFound,
          "Usuario no encontrado");
    }

    // Verificar si el usuario está eliminado (soft delete)
    if (user.IsDeleted)
    {
      return OperationResult.Fail<UpdateUserTypeAdminResponseDto>(
          ErrorInfo.Status422Business,
          "No se puede modificar un usuario eliminado");
    }

    // Un IsSuAdmin no cambia el tipo de una cuenta IsDevelopment (ver UserAccessHierarchy).
    if (!await UserAccessHierarchy.CanManageAsync(_authContextService, user))
    {
      return OperationResult.Fail<UpdateUserTypeAdminResponseDto>(ErrorInfo.Status403Forbidden, UserAccessHierarchy.InsufficientLevelMessage);
    }

    // Verificar si el usuario ya tiene el estado Admin solicitado
    if (user.IsAdmin == command.Request.IsAdmin)
    {
      return OperationResult.Fail<UpdateUserTypeAdminResponseDto>(
          ErrorInfo.Status422Business,
          $"El usuario ya {(command.Request.IsAdmin ? "es" : "no es")} Admin");
    }

    // Actualizar el estado Admin del usuario (UpdatedAt/UpdatedBy los pone AuditableEntitySaveChangesInterceptor con el usuario real)
    user.IsAdmin = command.Request.IsAdmin;

    await _repository.UpdateAsync(user, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogUserUpdatedSuccessfully(user.PublicId, $"Tipo Admin actualizado a: {command.Request.IsAdmin}");

    var response = new UpdateUserTypeAdminResponseDto(
        user.PublicId,
        user.Username,
        user.Email,
        user.IsAdmin,
        command.Request.Reason ?? "Actualización de tipo Admin");

    return OperationResult.Ok(response);
  }
}

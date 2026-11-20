// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// Handler para actualizar un rol existente a partir de un comando <see cref="UpdateRoleByPublicIdCommand"/>.
/// </summary>
/// <param name="repository">Repositorio de acceso a datos de roles (<see cref="IUpdateRoleByPublicIdRepository"/>).</param>
/// <param name="logger">Instancia de <see cref="IRoleLogger"/> para registrar eventos y errores relacionados con roles.</param>
/// <param name="unitOfWork">Unidad de trabajo para gestionar la transacción.</param>
/// <remarks>
/// Valida la existencia del rol, aplica reglas de negocio y persiste los cambios.
/// Utiliza <see cref="IUpdateRoleByPublicIdRepository"/> para acceso a datos y <see cref="IRoleLogger"/> para logging.
/// </remarks>
public class UpdateRoleByPublicIdHandler(IUpdateRoleByPublicIdRepository repository, IRoleLogger logger, IAutoDiscoveryIAMUnitOfWork unitOfWork)
{
  private readonly IUpdateRoleByPublicIdRepository _repository = repository;
  private readonly IRoleLogger _logger = logger;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

  /// <summary>
  /// Maneja la actualización de un rol existente.
  /// </summary>
  /// <param name="command">Comando con el PublicId y los nuevos datos del rol.</param>
  /// <param name="cancellationToken">Token para cancelar la operación.</param>
  /// <returns>Resultado de la operación y el rol actualizado en caso de éxito.</returns>
  public async Task<OperationResult<UpdateRoleByPublicIdResponseDto>> Handle(UpdateRoleByPublicIdCommand command, CancellationToken cancellationToken)
  {
    var role = await _repository.GetByPublicIdAsync(command.PublicId, cancellationToken);
    if (role == null)
    {
      _logger.LogRoleNotFoundByPublicId(command.PublicId);
      return OperationResult.Fail<UpdateRoleByPublicIdResponseDto>(
          ErrorInfo.Status404NotFound,
          "Rol no encontrado");
    }

    // Verificar si el rol está eliminado (soft delete)
    if (role.IsDeleted)
    {
      return OperationResult.Fail<UpdateRoleByPublicIdResponseDto>(
          ErrorInfo.Status422Business,
          "El rol está eliminado y no puede ser actualizado");
    }

    // Verificar si ya existe otro rol con el mismo nombre (excluyendo el actual)
    var existingRoleWithName = await _repository.GetByNameAsync(command.Request.Name, cancellationToken);
    if (existingRoleWithName != null && existingRoleWithName.Id != role.Id)
    {
      return OperationResult.Fail<UpdateRoleByPublicIdResponseDto>(
          ErrorInfo.Status409Conflict,
          "Ya existe un rol con el nombre especificado");
    }

    role.Name = command.Request.Name;
    role.Description = command.Request.Description;

    var success = await _repository.UpdateAsync(role, cancellationToken);
    if (success == null)
    {
      return OperationResult.Fail<UpdateRoleByPublicIdResponseDto>(
          ErrorInfo.Status422Business,
          "No se pudo actualizar el rol");
    }

    return await SaveAndBuildResponseAsync(role, cancellationToken);
  }

  private async Task<OperationResult<UpdateRoleByPublicIdResponseDto>> SaveAndBuildResponseAsync(
      Models.Entities.Role role,
      CancellationToken cancellationToken)
  {
    try
    {
      await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      return OperationResult.Fail<UpdateRoleByPublicIdResponseDto>(
          ErrorInfo.Status409Conflict,
          "El rol fue modificado por otro proceso mientras tanto. Recarga los datos e intenta de nuevo.");
    }

    _logger.LogRoleUpdated(role.Id, role.Name);

    var updatedRoleDto = new UpdateRoleByPublicIdResponseDto(
        role.PublicId,
        role.Name,
        role.Description,
        role.IsActive,
        role.CreatedAt,
        role.UpdatedAt);
    return OperationResult.Ok(updatedRoleDto);
  }
}

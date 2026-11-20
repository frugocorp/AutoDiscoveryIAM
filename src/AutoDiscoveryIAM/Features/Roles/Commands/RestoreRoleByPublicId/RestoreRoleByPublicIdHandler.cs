// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

/// <summary>
/// Handler para restaurar un rol eliminado a partir de un comando <see cref="RestoreRoleByPublicIdCommand"/>.
/// </summary>
/// <param name="repository">Repositorio de acceso a datos de roles (<see cref="IRestoreRoleByPublicIdRepository"/>).</param>
/// <param name="logger">Instancia de <see cref="IRoleLogger"/> para registrar eventos y errores relacionados con roles.</param>
/// <param name="unitOfWork">Unidad de trabajo para gestionar la transacción.</param>
/// <remarks>
/// Valida la existencia del rol eliminado, aplica reglas de negocio de restauración y persiste los cambios.
/// Utiliza <see cref="IRestoreRoleByPublicIdRepository"/> para acceso a datos y <see cref="IRoleLogger"/> para logging.
/// </remarks>
public class RestoreRoleByPublicIdHandler(IRestoreRoleByPublicIdRepository repository, IRoleLogger logger, IAutoDiscoveryIAMUnitOfWork unitOfWork)
{
    private readonly IRestoreRoleByPublicIdRepository _repository = repository;
    private readonly IRoleLogger _logger = logger;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Maneja la restauración de un rol eliminado.
    /// </summary>
    /// <param name="request">DTO de request con el PublicId del rol a restaurar.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Respuesta con el resultado de la operación y el rol restaurado en caso de éxito.</returns>
    public async Task<RestoreRoleByPublicIdResponse> Handle(RestoreRoleByPublicIdRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogRestoreRoleAttempt(request.PublicId);

        var deletedRole = await _repository.GetDeletedRoleByPublicIdAsync(request.PublicId, cancellationToken);
        if (deletedRole is null)
        {
            _logger.LogRoleNotFoundByPublicId(request.PublicId);
            return new RestoreRoleByPublicIdResponse(
                success: false,
                data: null,
                error: "Rol eliminado no encontrado",
                errorInfo: ErrorInfo.Status404NotFound);
        }

        var validationResult = await ValidateRestoreBusinessRules(deletedRole, request.PublicId, cancellationToken);
        if (validationResult != null)
        {
            return validationResult;
        }

        return await RestoreRoleAndBuildResponse(deletedRole, request.PublicId, cancellationToken);
    }

    private async Task<RestoreRoleByPublicIdResponse?> ValidateRestoreBusinessRules(
        Models.Entities.Role deletedRole,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var nameConflict = await _repository.ExistsActiveRoleWithNameAsync(deletedRole.Name, cancellationToken);
        if (nameConflict)
        {
            _logger.LogRestoreRoleNameConflict(publicId, deletedRole.Name);
            return new RestoreRoleByPublicIdResponse(
                success: false,
                data: null,
                error: $"Ya existe un rol activo con el nombre '{deletedRole.Name}'. No se puede restaurar.",
                errorInfo: ErrorInfo.Status409Conflict);
        }

        return null;
    }

    private async Task<RestoreRoleByPublicIdResponse> RestoreRoleAndBuildResponse(
        Models.Entities.Role deletedRole,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        // El campo RestoredBy se maneja automáticamente en AutoDiscoveryIAMDbContext
        var success = await _repository.RestoreRoleAsync(deletedRole, cancellationToken);

        if (success)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogRoleRestored(publicId, deletedRole.Name);

            var responseDto = new RestoreRoleByPublicIdResponseDto
            {
                PublicId = deletedRole.PublicId,
                Name = deletedRole.Name,
                Status = "Restaurado",
                RestoredAt = deletedRole.RestoredAt,
            };

            return new RestoreRoleByPublicIdResponse(
                success: true,
                data: responseDto);
        }

        return new RestoreRoleByPublicIdResponse(
            success: false,
            data: null,
            error: "No se pudo restaurar el rol",
            errorInfo: ErrorInfo.Status422Business);
    }
}
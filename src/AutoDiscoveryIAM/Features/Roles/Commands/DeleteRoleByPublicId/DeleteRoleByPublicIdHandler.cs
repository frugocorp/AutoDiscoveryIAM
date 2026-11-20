// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

/// <summary>
/// Handler para eliminar lógicamente un rol existente a partir de un comando <see cref="DeleteRoleByPublicIdCommand"/>.
/// </summary>
/// <param name="repository">Repositorio de acceso a datos de roles (<see cref="IDeleteRoleByPublicIdRepository"/>).</param>
/// <param name="logger">Instancia de <see cref="IRoleLogger"/> para registrar eventos y errores relacionados con roles.</param>
/// <param name="unitOfWork">Unidad de trabajo para gestionar la transacción.</param>
/// <remarks>
/// Valida la existencia del rol, aplica reglas de negocio de eliminación y persiste los cambios.
/// Utiliza <see cref="IDeleteRoleByPublicIdRepository"/> para acceso a datos y <see cref="IRoleLogger"/> para logging.
/// </remarks>
public class DeleteRoleByPublicIdHandler(IDeleteRoleByPublicIdRepository repository, IRoleLogger logger, IAutoDiscoveryIAMUnitOfWork unitOfWork)
{
    private readonly IDeleteRoleByPublicIdRepository _repository = repository;
    private readonly IRoleLogger _logger = logger;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Maneja la eliminación lógica de un rol existente.
    /// </summary>
    /// <param name="command">Comando con el PublicId del rol a eliminar.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Resultado de la operación y el rol eliminado en caso de éxito.</returns>
    public async Task<OperationResult<DeleteRoleByPublicIdResponseDto>> Handle(DeleteRoleByPublicIdCommand command, CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleByPublicIdAsync(command.PublicId, cancellationToken);
        if (role == null)
        {
            _logger.LogRoleNotFoundByPublicId(command.PublicId);
            return OperationResult.Fail<DeleteRoleByPublicIdResponseDto>(
                ErrorInfo.Status404NotFound,
                "Rol no encontrado");
        }

        // Verificar si el rol ya está eliminado
        if (role.IsDeleted)
        {
            return OperationResult.Fail<DeleteRoleByPublicIdResponseDto>(
                ErrorInfo.Status422Business,
                "El rol ya está eliminado");
        }

        // Verificar si el rol tiene usuarios asignados
        var hasActiveUsers = await _repository.HasActiveUsersAsync(role.Id, cancellationToken);
        if (hasActiveUsers)
        {
            return OperationResult.Fail<DeleteRoleByPublicIdResponseDto>(
                ErrorInfo.Status409Conflict,
                "No se puede eliminar el rol porque tiene usuarios asignados");
        }

        var success = await _repository.DeleteRoleAsync(role, cancellationToken);
        if (success)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogRoleDeleted(role.Id, true);
            var responseDto = new DeleteRoleByPublicIdResponseDto
            {
                PublicId = role.PublicId,
                Status = "Eliminado",
            };
            return OperationResult.Ok(responseDto);
        }

        return OperationResult.Fail<DeleteRoleByPublicIdResponseDto>(
            ErrorInfo.Status422Business,
            "No se pudo eliminar el rol");
    }
}
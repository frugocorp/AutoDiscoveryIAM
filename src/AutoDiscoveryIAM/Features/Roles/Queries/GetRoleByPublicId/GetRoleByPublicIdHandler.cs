// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// Handler para la query <see cref="GetRoleByPublicIdQuery"/>. Devuelve un rol por PublicId.
/// Solo expone PublicId, nunca el Id interno de la base de datos.
/// </summary>
public class GetRoleByPublicIdHandler(IGetRoleByPublicIdRepository repository, IRoleLogger logger)
{
    public async Task<OperationResult<GetRoleByPublicIdResponseDto>> Handle(GetRoleByPublicIdQuery query, CancellationToken cancellationToken)
    {
        var role = await repository.GetRoleByPublicId(query.PublicId);
        if (role == null)
        {
            logger.LogRoleNotFoundByPublicId(query.PublicId);
            return OperationResult.Fail<GetRoleByPublicIdResponseDto>(
                ErrorInfo.Status404NotFound,
                $"No se encontró el rol con PublicId {query.PublicId}");
        }

        logger.LogRoleRetrievedByPublicId(role.PublicId);

        var dto = new GetRoleByPublicIdResponseDto(
            role.PublicId,
            role.Name,
            role.Description,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt,
            role.DeletedAt,
            role.IsDeleted);

        return OperationResult.Ok(dto);
    }
}

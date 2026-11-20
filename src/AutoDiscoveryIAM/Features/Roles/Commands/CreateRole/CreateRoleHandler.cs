// -----------------------------------------------------------------------
// <copyright file="CreateRoleHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;

public class CreateRoleHandler(ICreateRoleRepository repository, IRoleLogger logger, IAutoDiscoveryIAMUnitOfWork unitOfWork)
{
    private readonly ICreateRoleRepository _repository = repository;
    private readonly IRoleLogger _logger = logger;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OperationResult<CreateRoleResponseDto>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        // Verificar si el nombre del rol ya existe
        if (await _repository.RoleNameExistsAsync(command.Name))
        {
            var errorMsg = $"Ya existe un rol con el nombre '{command.Name}'";
            _logger.LogRoleCreated(0, command.Name, false);
            return OperationResult.Fail<CreateRoleResponseDto>(ErrorInfo.Status422Business, errorMsg);
        }

        var role = new Role
        {
            Name = command.Name,
            Description = command.Description,
            IsActive = command.IsActive,
        };

        await _repository.CreateRole(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogRoleCreated(role.Id, role.Name);

        var responseDto = new CreateRoleResponseDto(role.PublicId, role.Name);
        return OperationResult.Ok(responseDto);
    }
}

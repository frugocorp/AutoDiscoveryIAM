// -----------------------------------------------------------------------
// <copyright file="CreateRoleCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;

public record CreateRoleCommand(string Name, string? Description, bool IsActive = true);

// -----------------------------------------------------------------------
// <copyright file="ExternalLogin.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;

/// <summary>
/// Vincula una identidad externa (proveedor + subject) con un usuario interno.
/// </summary>
public class ExternalLogin : AuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Hash SHA-256 del identificador de sujeto (<c>sub</c>) del proveedor externo —
    /// nunca se guarda en claro. Ver <see cref="FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.ExternalProviderIdHasher"/>.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    public virtual User User { get; set; } = null!;
}

// -----------------------------------------------------------------------
// <copyright file="IExternalLoginProvider.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Contracts;

public interface IExternalLoginProvider
{
    string ProviderName { get; }
    string BuildAuthorizationUrl(string state);
    Task<OperationResult<ExternalIdentityProfile>> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
}

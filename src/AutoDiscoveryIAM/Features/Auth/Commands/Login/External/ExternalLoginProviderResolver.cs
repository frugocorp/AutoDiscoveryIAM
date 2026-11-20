// -----------------------------------------------------------------------
// <copyright file="ExternalLoginProviderResolver.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Contracts;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;

public class ExternalLoginProviderResolver(IEnumerable<IExternalLoginProvider> providers)
{
    private readonly Dictionary<string, IExternalLoginProvider> _providers = providers
      .ToDictionary(x => x.ProviderName, StringComparer.OrdinalIgnoreCase);

    public IExternalLoginProvider? Resolve(string providerName)
    {
        return _providers.TryGetValue(providerName, out var provider) ? provider : null;
    }
}

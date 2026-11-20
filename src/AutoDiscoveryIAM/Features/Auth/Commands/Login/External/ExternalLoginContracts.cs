// -----------------------------------------------------------------------
// <copyright file="ExternalLoginContracts.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;

public static class ExternalLoginProviders
{
    public const string Google = "google";
}

public record ExternalIdentityProfile(
  string Provider,
  string ProviderId,
  string Email,
  bool IsEmailVerified);

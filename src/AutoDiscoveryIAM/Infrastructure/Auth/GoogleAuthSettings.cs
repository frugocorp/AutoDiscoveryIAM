// -----------------------------------------------------------------------
// <copyright file="GoogleAuthSettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;

public class GoogleAuthSettings
{
  public bool Enabled { get; set; }
  public string ClientId { get; set; } = string.Empty;
  public string ClientSecret { get; set; } = string.Empty;
  public string RedirectUri { get; set; } = string.Empty;
}

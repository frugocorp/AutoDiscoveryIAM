// -----------------------------------------------------------------------
// <copyright file="Command.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

public record LoginCommand(
  string Email,
  string Password,
  string? IpAddress = null,
  string? UserAgent = null);




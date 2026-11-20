// -----------------------------------------------------------------------
// <copyright file="ExchangeLoginCodeRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;

public record ExchangeLoginCodeRequestDto(string Code);

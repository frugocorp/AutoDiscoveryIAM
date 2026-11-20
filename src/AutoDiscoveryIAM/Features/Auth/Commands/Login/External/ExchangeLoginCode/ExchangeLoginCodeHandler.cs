// -----------------------------------------------------------------------
// <copyright file="ExchangeLoginCodeHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;

public class ExchangeLoginCodeHandler(IExternalLoginExchangeCodeRepository repository)
{
  private readonly IExternalLoginExchangeCodeRepository _repository = repository;

  public async Task<OperationResult<LoginResponseDto>> Handle(ExchangeLoginCodeCommand command, CancellationToken cancellationToken)
  {
    var session = await _repository.ConsumeAsync(command.Code, cancellationToken);
    if (session == null)
    {
      return OperationResult.Fail<LoginResponseDto>(
        ErrorInfo.Status401Unauthorized,
        "Código de intercambio inválido, ya usado o vencido.");
    }

    return OperationResult.Ok(session);
  }
}

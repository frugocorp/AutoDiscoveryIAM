// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;

/// <summary>
/// Primer paso del flujo de cambio de email: el usuario autenticado solicita un
/// nuevo email para sí mismo. No aplica el cambio — solo crea la solicitud en estado
/// <c>pending</c>, a la espera de revisión de un Admin.
/// </summary>
public class RequestEmailChangeHandler(
    IRequestEmailChangeRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork,
    IUserLogger logger)
{
  public async Task<OperationResult<RequestEmailChangeResponseDto>> Handle(RequestEmailChangeCommand command, CancellationToken cancellationToken)
  {
    var user = await repository.GetUserByPublicIdAsync(command.UserPublicId, cancellationToken);
    if (user == null)
    {
      return OperationResult.Fail<RequestEmailChangeResponseDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
    }

    if (string.Equals(user.Email, command.NewEmail, StringComparison.OrdinalIgnoreCase))
    {
      return OperationResult.Fail<RequestEmailChangeResponseDto>(
          ErrorInfo.Status422Business,
          "El nuevo email debe ser distinto al actual.");
    }

    // Que el email ya esté registrado NO se valida aquí: la respuesta delataría qué emails existen.
    // La solicitud queda pendiente igual, y al ejecutarla (ExecuteEmailChangeRequest) se rechaza si
    // el email ya pertenece a otra cuenta.
    var hasPendingRequest = await repository.HasPendingRequestAsync(user.Id, cancellationToken);
    if (hasPendingRequest)
    {
      return OperationResult.Fail<RequestEmailChangeResponseDto>(
          ErrorInfo.Status409Conflict,
          "Ya existe una solicitud de cambio de email pendiente para este usuario.");
    }

    var request = new EmailChangeRequest { UserId = user.Id, RequestedEmail = command.NewEmail };
    await repository.AddAsync(request, cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    logger.LogEmailChangeRequested(user.Id, command.NewEmail);

    return OperationResult.Ok(new RequestEmailChangeResponseDto(request.PublicId, request.RequestedEmail, request.Status, request.CreatedAt));
  }
}

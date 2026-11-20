// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

/// <summary>
/// Segundo paso del flujo de cambio de email: un Admin revisa la solicitud del
/// usuario, aprobandola para ejecucion (que un SuAdmin completará después) o rechazandola.
/// </summary>
public class ReviewEmailChangeRequestHandler(
    IReviewEmailChangeRequestRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork,
    IAuthContextService authContextService,
    IUserLogger logger)
{
  public async Task<OperationResult<ReviewEmailChangeRequestResponseDto>> Handle(ReviewEmailChangeRequestCommand command, CancellationToken cancellationToken)
  {
    var request = await repository.GetByPublicIdAsync(command.RequestPublicId, cancellationToken);
    if (request == null)
    {
      return OperationResult.Fail<ReviewEmailChangeRequestResponseDto>(ErrorInfo.Status404NotFound, "Solicitud de cambio de email no encontrada.");
    }

    if (request.Status != EmailChangeRequestStatusCatalog.Pending)
    {
      return OperationResult.Fail<ReviewEmailChangeRequestResponseDto>(
          ErrorInfo.Status409Conflict,
          "La solicitud ya fue revisada anteriormente.");
    }

    var reviewer = await authContextService.GetCurrentUserAsync()
      ?? throw new InvalidOperationException("No fue posible determinar el usuario autenticado que revisa la solicitud.");
    var reviewerId = reviewer.Id;

    request.Status = command.Approve ? EmailChangeRequestStatusCatalog.ApprovedForExecution : EmailChangeRequestStatusCatalog.Rejected;
    request.ReviewedBy = reviewerId;
    request.ReviewedAt = TimeHelpers.GetCurrentMexicoTime();
    request.RejectionReason = command.Approve ? null : command.RejectionReason;

    try
    {
      await unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      return OperationResult.Fail<ReviewEmailChangeRequestResponseDto>(
          ErrorInfo.Status409Conflict,
          "La solicitud fue modificada por otro proceso mientras tanto. Recarga los datos e intenta de nuevo.");
    }

    logger.LogEmailChangeReviewed(request.Id, reviewerId, command.Approve);

    return OperationResult.Ok(new ReviewEmailChangeRequestResponseDto(request.PublicId, request.Status, request.ReviewedAt, request.RejectionReason));
  }
}

// -----------------------------------------------------------------------
// <copyright file="ExecuteEmailChangeRequestHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;

/// <summary>
/// Tercer y ultimo paso del flujo de cambio de email: solo un SuAdmin ejecuta el
/// cambio real sobre <c>User.Email</c>, reiniciando la verificacion (<c>IsEmailVerified=false</c>)
/// y reenviando el correo de activacion, igual que en el registro inicial.
/// </summary>
public class ExecuteEmailChangeRequestHandler(
    IExecuteEmailChangeRequestRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork,
    IAuthContextService authContextService,
    IActivationService activationService,
    IActivationEmailService activationEmailService,
    IUserLogger logger)
{
  public async Task<OperationResult<ExecuteEmailChangeRequestResponseDto>> Handle(ExecuteEmailChangeRequestCommand command, CancellationToken cancellationToken)
  {
    var request = await repository.GetByPublicIdWithUserAsync(command.RequestPublicId, cancellationToken);
    if (request == null)
    {
      return OperationResult.Fail<ExecuteEmailChangeRequestResponseDto>(ErrorInfo.Status404NotFound, "Solicitud de cambio de email no encontrada.");
    }

    if (request.Status != EmailChangeRequestStatusCatalog.ApprovedForExecution)
    {
      return OperationResult.Fail<ExecuteEmailChangeRequestResponseDto>(
          ErrorInfo.Status409Conflict,
          "La solicitud debe estar aprobada para ejecucion antes de poder ejecutarse.");
    }

    var emailInUse = await repository.ExistsEmailAsync(request.RequestedEmail, request.UserId, cancellationToken);
    if (emailInUse)
    {
      return OperationResult.Fail<ExecuteEmailChangeRequestResponseDto>(ErrorInfo.Status409Conflict, UserValidationTypes.EmailExists.Message);
    }

    return await ApplyEmailChangeAsync(request, cancellationToken);
  }

  private async Task<OperationResult<ExecuteEmailChangeRequestResponseDto>> ApplyEmailChangeAsync(
      Models.Entities.EmailChangeRequest request,
      CancellationToken cancellationToken)
  {
    var user = request.User;
    user.Email = request.RequestedEmail;
    user.IsEmailVerified = false;
    var activationToken = activationService.AssignActivationToken(user);

    var sendResult = await activationEmailService.SendActivationEmailAsync(user, activationToken, cancellationToken, fakeMail: false);
    if (!sendResult.Success)
    {
      return OperationResult.Fail<ExecuteEmailChangeRequestResponseDto>(
          sendResult.ErrorInfo ?? ErrorInfo.Status424Dependency,
          sendResult.Error ?? "No fue posible enviar el correo de reverificacion.");
    }

    var executor = await authContextService.GetCurrentUserAsync()
      ?? throw new InvalidOperationException("No fue posible determinar el usuario autenticado que ejecuta el cambio.");
    var executorId = executor.Id;

    request.Status = EmailChangeRequestStatusCatalog.Executed;
    request.ExecutedBy = executorId;
    request.ExecutedAt = TimeHelpers.GetCurrentMexicoTime();

    try
    {
      await unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      return OperationResult.Fail<ExecuteEmailChangeRequestResponseDto>(
          ErrorInfo.Status409Conflict,
          "La solicitud fue modificada por otro proceso mientras tanto. Recarga los datos e intenta de nuevo.");
    }

    logger.LogEmailChangeExecuted(request.Id, user.Id, executorId);

    return OperationResult.Ok(new ExecuteEmailChangeRequestResponseDto(request.PublicId, user.PublicId, user.Email, request.Status, request.ExecutedAt));
  }
}

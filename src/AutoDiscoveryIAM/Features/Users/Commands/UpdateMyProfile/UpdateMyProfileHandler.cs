// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

/// <summary>
/// Handler de autoservicio: el usuario autenticado actualiza su propio PhoneNumber. Nunca opera
/// sobre otro usuario — el PublicId viene del JWT.
/// </summary>
public class UpdateMyProfileHandler(
    IUpdateMyProfileRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork,
    IUserLogger logger)
{
  public async Task<OperationResult<UpdateMyProfileResponseDto>> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
  {
    var user = await repository.GetByPublicIdAsync(command.UserPublicId, cancellationToken);
    if (user is null)
    {
      logger.LogUserNotFoundByPublicId(command.UserPublicId);
      return OperationResult.Fail<UpdateMyProfileResponseDto>(
          ErrorInfo.Status404NotFound,
          "No fue posible determinar el usuario autenticado.");
    }

    if (command.PhoneNumber != null)
    {
      user.PhoneNumber = command.PhoneNumber;
    }

    user.UpdatedAt = DateTime.UtcNow;

    try
    {
      await unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      return OperationResult.Fail<UpdateMyProfileResponseDto>(
          ErrorInfo.Status409Conflict,
          "Tu perfil fue modificado por otro proceso mientras tanto. Recarga los datos e intenta de nuevo.");
    }

    logger.LogUserUpdatedSuccessfully(user.PublicId, "Perfil actualizado por autoservicio");

    return OperationResult.Ok(new UpdateMyProfileResponseDto(
        user.PublicId,
        user.PhoneNumber,
        user.UpdatedAt));
  }
}

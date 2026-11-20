// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;

/// <summary>
/// Handler de autoservicio: el usuario autenticado elimina (soft-delete) su propia
/// cuenta. Nunca opera sobre otro usuario — el PublicId viene del JWT. Exige la contraseña actual:
/// un access token robado no alcanza para borrar la cuenta.
/// </summary>
public class DeleteMyAccountHandler(
    IDeleteMyAccountRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork,
    IUserLogger logger,
    IPasswordHasher passwordHasher)
{
  public async Task<OperationResult<DeleteMyAccountResponseDto>> Handle(DeleteMyAccountCommand command, CancellationToken cancellationToken)
  {
    var user = await repository.GetByPublicIdAsync(command.UserPublicId, cancellationToken);
    if (user is null)
    {
      logger.LogUserNotFoundByPublicId(command.UserPublicId);
      return OperationResult.Fail<DeleteMyAccountResponseDto>(
          ErrorInfo.Status404NotFound,
          "No fue posible determinar el usuario autenticado.");
    }

    if (string.IsNullOrWhiteSpace(command.CurrentPassword)
      || string.IsNullOrWhiteSpace(user.PasswordHash)
      || !passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash))
    {
      return OperationResult.Fail<DeleteMyAccountResponseDto>(
          ErrorInfo.Status401Unauthorized,
          UserValidationTypes.CurrentPasswordInvalid.Message);
    }

    repository.DeleteAccount(user);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    logger.LogUserDeleted(user.Id, user.Username);

    return OperationResult.Ok(new DeleteMyAccountResponseDto(user.PublicId, "Eliminado"));
  }
}

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using TuProyecto.Features.UserProfiles.Infrastructure.Logging;
using TuProyecto.Features.UserProfiles.Infrastructure.Validation;
using TuProyecto.Features.UserProfiles.Models.Entities;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public class CreateUserProfileHandler(ICreateUserProfileRepository repository, IUserProfileLogger logger, IAutoDiscoveryIAMUnitOfWork unitOfWork)
{
  private readonly ICreateUserProfileRepository _repository = repository;
  private readonly IUserProfileLogger _logger = logger;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

  public async Task<OperationResult<CreateUserProfileResponseDto>> Handle(CreateUserProfileCommand command, CancellationToken cancellationToken)
  {
    var userId = await _repository.GetUserIdByPublicIdAsync(command.UserPublicId);
    if (userId is null)
    {
      return OperationResult.Fail<CreateUserProfileResponseDto>(ErrorInfo.Status404NotFound, UserProfileValidationTypes.UserPublicIdMessages.NotFound);
    }

    var userProfile = new UserProfile
    {
      UserId = userId.Value,
      Occupation = command.Occupation,
      Biography = command.Biography,
      MaritalStatus = command.MaritalStatus,
    };

    await _repository.CreateUserProfile(userProfile);

    // IAutoDiscoveryIAMUnitOfWork de la librería, no uno propio: en modo contexto único
    // ApplicationDbContext ES (por herencia) un AutoDiscoveryIAMDbContext, así que guardar aquí
    // persiste también UserProfiles -- es literalmente la misma instancia de DbContext. Ver "Elige
    // tu propio DbContext: uno o dos" en el README de AutoDiscoveryIAM; con dos DbContexts
    // separados sí necesitarías tu propio Unit of Work para el tuyo.
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogUserProfileCreated(userProfile.Id, userProfile.UserId);

    var responseDto = new CreateUserProfileResponseDto(userProfile.PublicId, command.UserPublicId, userProfile.Occupation, userProfile.Biography, userProfile.MaritalStatus);
    return OperationResult.Ok(responseDto);
  }
}

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;

public interface IUserLogger : ILoggerBase<IUserLogger>
{
  void LogUserCreated(long userId, string username, string email, bool success = true);
  void LogUserUpdated(long userId, string username, bool success = true);
  void LogUserDeleted(long userId, string username, bool success = true);
  void LogUserRestored(long userId, string username, bool success = true);
  void LogUserNotFound(long userId);
  void LogUserNotFoundByPublicId(Guid publicId);
  void LogUserUpdatedSuccessfully(Guid publicId, string details);
  void LogUsersRetrieved(int count);
  void LogUserRetrieved(long userId);
  void LogPasswordChanged(long userId, bool success = true);
  void LogPasswordResetRequested(string email, bool success = true);
  void LogPasswordResetConfirmed(long userId, bool success = true);
  void LogEmailChangeRequested(long userId, string requestedEmail);
  void LogEmailChangeReviewed(long requestId, long reviewedBy, bool approved);
  void LogEmailChangeExecuted(long requestId, long userId, long executedBy);
}

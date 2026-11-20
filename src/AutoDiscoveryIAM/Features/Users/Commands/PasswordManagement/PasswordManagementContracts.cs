using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

public sealed class PasswordResetSettings
{
    public int TokenLength { get; set; } = 64;
    public int TokenExpiryMinutes { get; set; } = 30;

    /// <summary>
    /// Duración mínima de la respuesta de <c>POST /users/password/reset/request</c>, exista o no la
    /// cuenta: si es menor que lo que tarda tu servidor SMTP, el tiempo de respuesta delata qué
    /// correos están registrados.
    /// </summary>
    public int MinimumResponseMilliseconds { get; set; } = 1000;
}

public sealed record ChangePasswordRequestDto(string? CurrentPassword, string NewPassword);
public sealed record RequestPasswordResetRequestDto(string Email);
public sealed record ConfirmPasswordResetRequestDto(string Token, string NewPassword);

public sealed record PasswordManagementStatusDto(string Message);

public sealed record PasswordManagementResponse(
  bool Success,
  PasswordManagementStatusDto? Data,
  string? Error = null,
  FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ErrorInfo? ErrorInfo = null) : ApiResponse<PasswordManagementStatusDto>(Success, Data, Error, ErrorInfo);

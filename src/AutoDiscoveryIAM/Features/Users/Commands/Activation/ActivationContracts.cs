using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;

public sealed class ActivationSettings
{
    public int EmailVerificationTokenLength { get; set; } = 64;
    public int EmailVerificationTokenExpiryMinutes { get; set; } = 30;
    public int ActivationResendCooldownSeconds { get; set; } = 60;
    public int ActivationResendMaxPerHour { get; set; } = 5;

    /// <summary>
    /// Duración mínima de la respuesta de <c>POST /users/activation/resend</c>, exista o no la cuenta: si es menor que lo que tarda tu servidor SMTP, el
    /// tiempo de respuesta delata qué correos están registrados.
    /// </summary>
    public int MinimumResponseMilliseconds { get; set; } = 1000;
}

public sealed record ActivationConfirmRequestDto(string Token);
public sealed record ActivationResendRequestDto(string Email);
public sealed record ActivationTestEmailRequestDto(string? Subject = null, string? Body = null, bool FakeMail = false);

public sealed record ActivationStatusDto(
  string Email,
  bool IsActivated,
  string Message,
  string LoginUrl);

public sealed record ActivationTestEmailResponseDto(string Message);

public sealed record ActivationResponse(
  bool Success,
  ActivationStatusDto? Data,
  string? Error = null,
  FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ErrorInfo? ErrorInfo = null) : ApiResponse<ActivationStatusDto>(Success, Data, Error, ErrorInfo);

public sealed record ActivationAdminResponse(
  bool Success,
  ActivationStatusDto? Data,
  string? Error = null,
  FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ErrorInfo? ErrorInfo = null) : ApiResponse<ActivationStatusDto>(Success, Data, Error, ErrorInfo);

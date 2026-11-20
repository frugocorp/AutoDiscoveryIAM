using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;

public sealed record EnableEmailTwoFactorRequestDto(string CurrentPassword, string? Code = null);
public sealed record EnrollAuthenticatorRequestDto(string CurrentPassword, string? Code = null);
public sealed record ConfirmAuthenticatorRequestDto(string Code);
public sealed record DisableTwoFactorRequestDto(string CurrentPassword, string? Code = null);

public sealed record TwoFactorStatusDto(string Method, bool Enabled, string Message);
public sealed record EnrollAuthenticatorResponseDto(string Secret, string ProvisioningUri);

public sealed record TwoFactorResponse(
  bool Success,
  TwoFactorStatusDto? Data,
  string? Error = null,
  FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ErrorInfo? ErrorInfo = null) : ApiResponse<TwoFactorStatusDto>(Success, Data, Error, ErrorInfo);

public sealed record EnrollAuthenticatorResponse(
  bool Success,
  EnrollAuthenticatorResponseDto? Data,
  string? Error = null,
  FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ErrorInfo? ErrorInfo = null) : ApiResponse<EnrollAuthenticatorResponseDto>(Success, Data, Error, ErrorInfo);

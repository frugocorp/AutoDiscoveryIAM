// -----------------------------------------------------------------------
// <copyright file="GetUsersOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers.Docs;

public sealed class GetUsersOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Usuarios recuperados correctamente (respuesta paginada).";

  protected override object SuccessExample { get; } = new ApiResponse<PagedResultDto<GetUsersResponseDto>>(
    true,
    new PagedResultDto<GetUsersResponseDto>(
      [
        new GetUsersResponseDto(
            PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
            Username: "admin",
            Email: "admin@example.com",
            IsActive: true,
            IsAdmin: true,
            IsDevelopment: false,
            IsSuAdmin: false),
        new GetUsersResponseDto(
            PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
            Username: "user",
            Email: "user@example.com",
            IsActive: true,
            IsAdmin: false,
            IsDevelopment: false,
            IsSuAdmin: false),
      ],
      PageNumber: 1,
      PageSize: 20,
      TotalCount: 2),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
  };
}

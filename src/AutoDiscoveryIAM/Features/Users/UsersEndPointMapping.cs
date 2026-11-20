// -----------------------------------------------------------------------
// <copyright file="UsersEndPointMapping.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users;

public static class UsersEndPointMapping
{
  public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
  {
    GetUsersEndPoint.MapGetUsersEndpoint(app);
    GetUserByPublicIdEndPoint.MapGetUserByPublicIdEndpoint(app);
    GetMyPermissionsEndPoint.MapGetMyPermissionsEndpoint(app);
    GetRolesByPublicIdEndPoint.MapGetRolesByPublicIdEndpoint(app);
    GetPermissionsByPublicIdEndPoint.MapGetPermissionsByPublicIdEndpoint(app);
    CreateUserEndPoint.MapCreateUserEndpoint(app);
    ActivationEndPoint.MapActivationEndpoints(app);
    PasswordManagementEndPoint.MapPasswordManagementEndpoints(app);
    TwoFactorEndPoint.MapTwoFactorEndpoints(app);
    CreateUserRolesEndPoint.MapCreateUserRolesEndpoint(app);
    DeleteUserRolesByPublicIdEndPoint.MapDeleteUserRolesByPublicIdEndpoint(app);
    UpdateBulkUserRolesByRolePublicIdEndPoint.MapUpdateBulkUserRolesByRolePublicIdEndPoint(app);
    UpdateBulkUserRolesEndPoint.MapUpdateBulkUserRolesEndpoint(app);
    UpdateUserEndPoint.MapUpdateUserEndpoint(app);
    UpdateMyProfileEndPoint.MapUpdateMyProfileEndpoint(app);
    DeleteMyAccountEndPoint.MapDeleteMyAccountEndpoint(app);
    UpdateUserTypeAdminEndPoint.MapUpdateUserTypeAdminEndpoint(app);
    RequestEmailChangeEndPoint.MapRequestEmailChangeEndpoint(app);
    ReviewEmailChangeRequestEndPoint.MapReviewEmailChangeRequestEndpoint(app);
    ExecuteEmailChangeRequestEndPoint.MapExecuteEmailChangeRequestEndpoint(app);
  }
}

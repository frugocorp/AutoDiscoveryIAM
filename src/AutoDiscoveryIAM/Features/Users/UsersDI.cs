// -----------------------------------------------------------------------
// <copyright file="UsersDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.DefaultRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users;

public static class UsersDI
{
  public static IServiceCollection AddUsersDI(this IServiceCollection services)
  {
    services.AddTransient<IUserLogger, UserLogger>();
    services.AddActivationDI();
    services.AddPasswordManagementDI();
    services.AddTwoFactorDI();
    services.AddCreateUserDI();
    services.AddCreateUserByAdminDI();
    services.AddCreateUserRolesDI();
    services.AddDeleteUserRolesByPublicIdDI();
    services.AddRestoreUserRolesByPublicIdDI();
    services.AddUpdateBulkUserRolesDI();
    services.AddUpdateBulkUserRolesByRolePublicIdDI();
    services.AddUpdateUserDI();
    services.AddUpdateMyProfileDI();
    services.AddDeleteMyAccountDI();
    services.AddUpdateUserTypeAdminDI();
    services.AddGetUsersDI();
    services.AddGetUserByPublicIdDI();
    services.AddGetMyPermissionsDI();
    services.AddGetRolesByPublicIdDI();
    services.AddGetPermissionsByPublicIdDI();
    services.AddRequestEmailChangeDI();
    services.AddReviewEmailChangeRequestDI();
    services.AddExecuteEmailChangeRequestDI();

    // Servicios de consulta de usuarios
    services.AddScoped<IUserQueryService, UserQueryService>();

    // Rol por defecto para todo usuario nuevo, sin importar el camino que lo crea
    services.AddScoped<IDefaultRoleAssignmentService, DefaultRoleAssignmentService>();

    return services;
  }
}

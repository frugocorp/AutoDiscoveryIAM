// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;

public static class DeleteMyAccountDI
{
  public static IServiceCollection AddDeleteMyAccountDI(this IServiceCollection services)
  {
    services.AddScoped<IDeleteMyAccountRepository, DeleteMyAccountRepository>();
    services.AddScoped<DeleteMyAccountHandler>();
    return services;
  }
}

// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

public static class UpdateMyProfileDI
{
  public static IServiceCollection AddUpdateMyProfileDI(this IServiceCollection services)
  {
    services.AddScoped<IUpdateMyProfileRepository, UpdateMyProfileRepository>();
    services.AddScoped<UpdateMyProfileHandler>();
    services.AddScoped<UpdateMyProfileCommandValidator>();
    return services;
  }
}

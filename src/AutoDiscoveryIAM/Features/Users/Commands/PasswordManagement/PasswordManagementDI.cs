namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

public static class PasswordManagementDI
{
    public static IServiceCollection AddPasswordManagementDI(this IServiceCollection services)
    {
        services.AddScoped<IPasswordManagementRepository, PasswordManagementRepository>();
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
        services.AddScoped<IPasswordResetEmailService, PasswordResetEmailService>();
        services.AddScoped<IPasswordManagementService, PasswordManagementService>();
        services.AddScoped<ChangePasswordRequestValidator>();
        services.AddScoped<RequestPasswordResetRequestValidator>();
        services.AddScoped<ConfirmPasswordResetRequestValidator>();
        return services;
    }
}

using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.Provisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Services.Provisioning;

/// <summary>
/// Pruebas de integración del alta desde el código del consumidor, contra la base real.
/// </summary>
[Collection("GlobalTestSetup")]
public class UserProvisioningServiceIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string Password = "TestPass123*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  private static (string Username, string Email) NewIdentity(string prefix)
  {
    var suffix = Guid.NewGuid().ToString("N")[..12];
    return ($"{prefix}_{suffix}", $"{prefix}_{suffix}@example.com");
  }

  private async Task<bool> UserExistsAsync(string email)
  {
    using var scope = factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    return await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, _cancellationToken);
  }

  [Fact(DisplayName = "Servicio de alta: crea el usuario con el rol por defecto, correo sin verificar y cambio de contraseña obligatorio")]
  public async Task CreateAsync_Valid_CreatesUserWithAdiamRules()
  {
    var (username, email) = NewIdentity("prov");
    using var scope = factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

    var result = await service.CreateAsync(new ProvisionUserRequest(username, email, Password, true), _cancellationToken);

    result.Success.Should().BeTrue();
    using var check = factory.Services.CreateScope();
    var user = await check.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>().Users
      .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
      .SingleAsync(u => u.PublicId == result.Data!.PublicId, _cancellationToken);
    user.IsActive.Should().BeTrue();
    user.IsEmailVerified.Should().BeFalse();
    user.MustChangePassword.Should().BeTrue();
    user.UserRoles.Count(ur => ur.Role.IsDefaultForNewUser).Should().Be(1, "se asigna el rol por defecto");
  }

  [Fact(DisplayName = "Servicio de alta: una contraseña débil responde 400 y no crea nada")]
  public async Task CreateAsync_WeakPassword_ReturnsValidationError()
  {
    var (username, email) = NewIdentity("provweak");
    using var scope = factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

    var result = await service.CreateAsync(new ProvisionUserRequest(username, email, "123", true), _cancellationToken);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
    (await UserExistsAsync(email)).Should().BeFalse();
  }

  [Fact(DisplayName = "Servicio de alta: si el enlace del consumidor falla no queda el usuario, ni con un guardado posterior")]
  public async Task CreateAsync_WhenBeforeSaveThrows_LeavesNoUser()
  {
    var (username, email) = NewIdentity("provfail");
    using var scope = factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();
    var context = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    Func<Task> act = () => service.CreateAsync(
      new ProvisionUserRequest(username, email, Password, true),
      (user, _) => throw new InvalidOperationException("falla la entidad del consumidor"),
      _cancellationToken);

    (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("falla la entidad del consumidor");
    await context.SaveChangesAsync(_cancellationToken);
    (await UserExistsAsync(email)).Should().BeFalse("el usuario salió del contexto al fallar el alta");
  }

  [Fact(DisplayName = "Servicio de alta: el enlace del consumidor recibe al usuario sin guardar y todo se guarda junto")]
  public async Task CreateAsync_WithBeforeSave_SavesOnce()
  {
    var (username, email) = NewIdentity("provlink");
    using var scope = factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();
    var context = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var stateSeen = EntityState.Detached;

    var result = await service.CreateAsync(
      new ProvisionUserRequest(username, email, Password, true),
      (user, _) =>
      {
        stateSeen = context.Entry(user).State;
        return Task.CompletedTask;
      },
      _cancellationToken);

    result.Success.Should().BeTrue();
    stateSeen.Should().Be(EntityState.Added, "el consumidor enlaza su entidad antes del único guardado");
    (await UserExistsAsync(email)).Should().BeTrue();
  }
}

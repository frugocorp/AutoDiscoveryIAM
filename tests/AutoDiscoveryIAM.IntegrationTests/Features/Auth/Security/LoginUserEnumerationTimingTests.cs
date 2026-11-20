// -----------------------------------------------------------------------
// <copyright file="LoginUserEnumerationTimingTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Security;

/// <summary>
/// <c>LoginHandler</c> ya respondía con el mismo mensaje ("Credenciales inválidas.") tanto si el
/// email no existe como si la contraseña es incorrecta -- la enumeración por mensaje ya estaba
/// resuelta. Pero solo el camino de "contraseña incorrecta" ejecutaba <c>VerifyPassword</c>
/// (Argon2id, deliberadamente lento); el de "email no existe" retornaba casi instantáneo,
/// delatando por tiempo de respuesta si un email está registrado. Este test confirma que, tras
/// agregar una verificación Argon2id dummy en el camino de "no existe" (<c>LoginHandler</c>), las
/// dos rutas tardan tiempos comparables, no exactos: la comparación usa el promedio de varias
/// muestras para tolerar el ruido normal de un test de integración.
/// </summary>
[Collection("GlobalTestSetup")]
[Trait("Category", "Security")]
public class LoginUserEnumerationTimingTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const int SampleCount = 5;
  private const string RealUserPasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$OOwzesn4eOhertDDWp7BEQ$q3C9LZDkbhpIUeH8c1Sxi2ailXMeVnP3EEQ2rs3K7dU";

  private readonly CustomWebApplicationFactory<Program> _factory = factory;
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

  [Fact(DisplayName = "Login: email inexistente y contraseña incorrecta de un email real tardan tiempos comparables (sin fuga por timing)")]
  public async Task Login_NonExistentEmail_TakesComparableTimeToWrongPasswordOnRealEmail()
  {
    // Usuario dedicado y desechable, exclusivo de este test: 5 intentos con contraseña incorrecta
    // alcanzan MaxFailedAttempts y bloquean la cuenta -- reusar un usuario sembrado compartido
    // (ej. user@example.com) dejaría esa cuenta bloqueada para el resto de la suite.
    var realEmail = $"enum-timing-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(realEmail);

    var nonExistentEmailDurationsMs = await MeasureLoginAttemptsAsync($"does-not-exist-{Guid.NewGuid():N}@example.com", "WrongPassword123!");
    var wrongPasswordDurationsMs = await MeasureLoginAttemptsAsync(realEmail, "WrongPassword123!");

    var averageNonExistent = nonExistentEmailDurationsMs.Average();
    var averageWrongPassword = wrongPasswordDurationsMs.Average();

    // Tolerancia generosa (no una igualdad exacta): antes del fix, el email inexistente respondía
    // en pocos milisegundos frente a las decenas/cientos de milisegundos de un Argon2id real -- una
    // diferencia de un orden de magnitud. Un factor de 3x o una diferencia absoluta bajo 150ms sigue
    // siendo una defensa razonable contra la enumeración, sin que el test sea fràgil en CI.
    var ratio = Math.Max(averageNonExistent, averageWrongPassword) / Math.Max(1, Math.Min(averageNonExistent, averageWrongPassword));
    var absoluteDifference = Math.Abs(averageNonExistent - averageWrongPassword);

    (ratio <= 3.0 || absoluteDifference <= 150)
      .Should().BeTrue($"la diferencia de tiempo entre email inexistente ({averageNonExistent:0}ms) y contraseña incorrecta en email real ({averageWrongPassword:0}ms) no debería delatar si el email existe");
  }

  private async Task<List<double>> MeasureLoginAttemptsAsync(string email, string password)
  {
    var durations = new List<double>();
    var loginRequest = new LoginRequestDto(email, password);

    for (var i = 0; i < SampleCount; i++)
    {
      var stopwatch = System.Diagnostics.Stopwatch.StartNew();
      using var response = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
      stopwatch.Stop();
      durations.Add(stopwatch.Elapsed.TotalMilliseconds);
    }

    return durations;
  }

  private async Task CreateActiveUserAsync(string email)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    db.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"enum-timing-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = RealUserPasswordHash,
      Status = "active",
      IsActive = true,
      IsDeleted = false,
    });

    await db.SaveChangesAsync(_cancellationToken);
  }
}

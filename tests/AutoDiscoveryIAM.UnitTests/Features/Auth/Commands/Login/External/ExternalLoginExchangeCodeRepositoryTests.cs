using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.External;

public class ExternalLoginExchangeCodeRepositoryTests
{
  private static AutoDiscoveryIAMDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new AutoDiscoveryIAMDbContext(options);
  }

  [Fact]
  public async Task CreateAsync_ShouldPersistOnlyTheHashedCode_NeverThePlaintext()
  {
    await using var context = CreateContext();
    var repository = new ExternalLoginExchangeCodeRepository(context);
    var session = new LoginResponseDto("jwt-access-token", DateTime.UtcNow.AddMinutes(15), "plaintext-refresh-token");

    var plaintextCode = await repository.CreateAsync(session, TimeSpan.FromSeconds(60), CancellationToken.None);

    plaintextCode.Should().NotBeNullOrWhiteSpace();
    var stored = await context.ExternalLoginExchangeCodes.SingleAsync(CancellationToken.None);
    stored.CodeHash.Should().NotBe(plaintextCode);
    stored.CodeHash.Should().Be(ExternalLoginExchangeCodeHasher.Hash(plaintextCode));
    stored.IsConsumed.Should().BeFalse();
  }

  [Fact(DisplayName = "Dos canjes simultáneos del mismo código: solo uno recibe la sesión")]
  public async Task ConsumeAsync_ConcurrentConsumption_OnlyOneSucceeds()
  {
    var databaseName = Guid.NewGuid().ToString();
    DbContextOptions<AutoDiscoveryIAMDbContext> Options() => new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName)
      .Options;
    var session = new LoginResponseDto("jwt-access-token", DateTime.UtcNow.AddMinutes(15), "plaintext-refresh-token");

    string plaintextCode;
    await using (var setup = new AutoDiscoveryIAMDbContext(Options()))
    {
      plaintextCode = await new ExternalLoginExchangeCodeRepository(setup).CreateAsync(session, TimeSpan.FromSeconds(60), CancellationToken.None);
    }

    await using var slowRequest = new AutoDiscoveryIAMDbContext(Options());
    await using var fastRequest = new AutoDiscoveryIAMDbContext(Options());

    // La petición lenta ya leyó el código (sin consumir) cuando la rápida lo consume.
    await slowRequest.ExternalLoginExchangeCodes.SingleAsync(CancellationToken.None);
    var fastResult = await new ExternalLoginExchangeCodeRepository(fastRequest).ConsumeAsync(plaintextCode, CancellationToken.None);
    var slowResult = await new ExternalLoginExchangeCodeRepository(slowRequest).ConsumeAsync(plaintextCode, CancellationToken.None);

    fastResult.Should().NotBeNull();
    slowResult.Should().BeNull("el código es de un solo uso aunque dos peticiones lo canjeen a la vez");
  }

  [Fact]
  public async Task ConsumeAsync_WithValidCode_ShouldReturnSessionAndMarkAsConsumed()
  {
    await using var context = CreateContext();
    var repository = new ExternalLoginExchangeCodeRepository(context);
    var session = new LoginResponseDto("jwt-access-token", DateTime.UtcNow.AddMinutes(15), "plaintext-refresh-token");
    var plaintextCode = await repository.CreateAsync(session, TimeSpan.FromSeconds(60), CancellationToken.None);

    var result = await repository.ConsumeAsync(plaintextCode, CancellationToken.None);

    result.Should().NotBeNull();
    result!.Token.Should().Be(session.Token);
    result.RefreshToken.Should().Be(session.RefreshToken);

    var stored = await context.ExternalLoginExchangeCodes.SingleAsync(CancellationToken.None);
    stored.IsConsumed.Should().BeTrue();
    stored.ConsumedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task ConsumeAsync_WhenCodeAlreadyConsumed_ShouldReturnNull()
  {
    await using var context = CreateContext();
    var repository = new ExternalLoginExchangeCodeRepository(context);
    var session = new LoginResponseDto("jwt-access-token", DateTime.UtcNow.AddMinutes(15), "plaintext-refresh-token");
    var plaintextCode = await repository.CreateAsync(session, TimeSpan.FromSeconds(60), CancellationToken.None);
    await repository.ConsumeAsync(plaintextCode, CancellationToken.None);

    var secondAttempt = await repository.ConsumeAsync(plaintextCode, CancellationToken.None);

    secondAttempt.Should().BeNull("un código de un solo uso no debe poder canjearse dos veces");
  }

  [Fact]
  public async Task ConsumeAsync_WhenCodeExpired_ShouldReturnNull()
  {
    await using var context = CreateContext();
    var repository = new ExternalLoginExchangeCodeRepository(context);
    var session = new LoginResponseDto("jwt-access-token", DateTime.UtcNow.AddMinutes(15), "plaintext-refresh-token");
    var plaintextCode = await repository.CreateAsync(session, TimeSpan.FromSeconds(-1), CancellationToken.None);

    var result = await repository.ConsumeAsync(plaintextCode, CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task ConsumeAsync_WithUnknownCode_ShouldReturnNull()
  {
    await using var context = CreateContext();
    var repository = new ExternalLoginExchangeCodeRepository(context);

    var result = await repository.ConsumeAsync("codigo-que-nunca-existio", CancellationToken.None);

    result.Should().BeNull();
  }
}

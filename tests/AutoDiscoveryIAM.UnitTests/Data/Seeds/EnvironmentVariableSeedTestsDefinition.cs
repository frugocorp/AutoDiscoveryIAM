using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Data.Seeds;

/// <summary>
/// Agrupa las pruebas que manipulan la variable de entorno global ASPNETCORE_ENVIRONMENT
/// (DevUserSeeder/UserSeeder/UserRoleSeeder/GateSpecialUserSeedsToDevelopment), para que xUnit
/// las ejecute secuencialmente y no corran en paralelo pisándose el valor del proceso.
/// </summary>
[CollectionDefinition("EnvironmentVariableSeedTests", DisableParallelization = true)]
public class EnvironmentVariableSeedTestsDefinition
{
}

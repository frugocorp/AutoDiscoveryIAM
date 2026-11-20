// -----------------------------------------------------------------------
// <copyright file="TestEnvSetup.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using DotNetEnv;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests
{
  /// <summary>
  /// Define la colección de pruebas global <c>GlobalTestSetup</c> para xUnit, deshabilitando la ejecución en paralelo.
  /// </summary>
  /// <remarks>
  /// Esta colección utiliza el fixture <c>EnvFixture</c> para garantizar que las variables de entorno necesarias se carguen antes de ejecutar cualquier prueba.
  /// El propósito es compartir el entorno de configuración entre todas las clases de prueba que pertenecen a esta colección.
  /// </remarks>
  [CollectionDefinition("GlobalTestSetup", DisableParallelization = true)]
  public class GlobalTestSetup : ICollectionFixture<EnvFixture>
  {
    // Garantiza que el fixture se ejecute antes de cualquier test.
  }

  /// <summary>
  /// Fixture de entorno para pruebas que carga variables de entorno desde un archivo <c>.env</c>.
  /// </summary>
  /// <remarks>
  /// Este fixture se ejecuta una sola vez antes de cualquier prueba de la colección <c>GlobalTestSetup</c>.
  /// Carga el archivo <c>.env</c> del directorio actual y expone las variables de entorno para su uso en las pruebas.
  /// Además, imprime en consola el valor de la variable <c>USE_REAL_DB_FOR_TESTS</c> para trazabilidad.
  /// </remarks>
  public class EnvFixture
  {
    private static bool loaded = false;
    const bool consoleWriteLine = false;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <c>EnvFixture</c>.
    /// </summary>
    /// <remarks>
    /// Este constructor carga el archivo <c>.env</c> del directorio actual y establece las variables de entorno necesarias para las pruebas.
    /// La carga del archivo se realiza solo una vez, independientemente de cuántas veces se instancie el fixture.
    /// Además, imprime en consola la ruta del archivo <c>.env</c> y el valor de la variable <c>USE_REAL_DB_FOR_TESTS</c> para facilitar la trazabilidad durante la ejecución de las pruebas.
    /// </remarks>
    public EnvFixture()
    {
      if (!loaded)
      {
        // Carga el archivo .env del directorio actual
        var path = System.IO.Path.GetFullPath(".env");
        if (consoleWriteLine) Console.WriteLine($"********* Intentando cargar .env desde: {path}");
        DotNetEnv.Env.Load(path);
        var testVar = Environment.GetEnvironmentVariable("USE_REAL_DB_FOR_TESTS");
        if (consoleWriteLine) Console.WriteLine($"********* EnvFixture USE_REAL_DB_FOR_TESTS: {testVar}");
        loaded = true;
      }
    }
  }
}

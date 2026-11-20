// -----------------------------------------------------------------------
// <copyright file="DatabaseInitialization.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.SampleHost.Infrastructure;

/// <summary>
/// Espera a que la base de datos esté disponible (con reintentos) y, solo en Development, aplica
/// migraciones pendientes al arrancar.
/// </summary>
/// <remarks>
/// Opcional, y no forma parte ni de la librería ni de "la app": es infraestructura propia de este
/// host. Existe porque el contenedor de SQL Server (<c>docker/db/</c>) es un stack de Docker
/// independiente del de la API, sin <c>depends_on</c>/<c>healthcheck</c> entre ambos (ver
/// <c>docker/README.md</c>) -- a propósito, para que cada stack se pueda usar por separado (ej. la
/// API contra tu propia SQL Server remota, sin el contenedor de <c>docker/db/</c>). Sin ese enlace
/// de Docker, nada garantiza que la base de datos ya acepte conexiones cuando arranca este proceso
/// -- el reintento de abajo es lo único que cubre esa condición de carrera al levantar
/// <c>docker-compose up</c> desde cero.
/// <para>
/// Un equipo que prefiera no depender de esto (porque su propio orquestador ya garantiza el orden
/// de arranque, o porque aplica migraciones a mano con <c>dotnet ef database update</c> antes de
/// levantar la app) puede simplemente no llamar a <see cref="InitializeDatabaseAsync"/> y quedarse
/// con un <c>AddDbContext&lt;AutoDiscoveryIAMDbContext&gt;(...)</c> normal, sin ninguna lógica
/// adicional en <c>Program.cs</c> -- no es un requisito de <c>AddAutoDiscoveryIAM()</c> ni de
/// <c>UseAutoDiscoveryIAM()</c>.
/// </para>
/// </remarks>
public static class DatabaseInitializationExtensions
{
  public static async Task InitializeDatabaseAsync(this WebApplication app)
  {
    // Se crea un scope para resolver servicios con ciclo de vida 'scoped' como DbContext.
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var logger = services.GetRequiredService<ILoggerBase<Program>>();
    const int maxRetries = 10;
    var retryCount = 0;
    var waitTime = TimeSpan.FromSeconds(5);

    logger.LogInformation("Intentando conectar a la base de datos...", null);

    while (retryCount < maxRetries)
    {
      try
      {
        logger.LogInformation("Obteniendo DbContext para inicialización...", null);
        var dbContext = services.GetRequiredService<AutoDiscoveryIAMDbContext>();
        logger.LogInformation("DbContext obtenido correctamente. Validación de conexión exitosa...", null);

        // Solo en Development: aplica migraciones pendientes al arrancar. Idempotente (EF Core
        // consulta __EFMigrationsHistory y omite lo ya aplicado). En Producción sigue sin correr
        // automáticamente; se aplica a mano (ver README.md).
        if (app.Environment.IsDevelopment())
        {
          logger.LogInformation("Aplicando migraciones pendientes...", null);
          await dbContext.Database.MigrateAsync(CancellationToken.None);
        }

        return; // Éxito
      }
      catch (Exception ex) // Capturar excepciones específicas como SqlException si es posible
      {
        retryCount++;
        logger.LogWarning($"Fallo la conexión a la base de datos. Intento {retryCount} de {maxRetries}. Esperando {waitTime.TotalSeconds}s...", ex);

        if (retryCount == maxRetries)
        {
          logger.LogError($"No se pudo conectar a la base de datos después de {maxRetries} intentos.", ex);

          // Agotar los reintentos debe detener el arranque (fail-hard), no dejar que la app siga
          // "rota" en silencio sin forma de que un orquestador (Docker/K8s) lo detecte.
          throw;
        }

        await Task.Delay(waitTime);
      }
    }
  }
}

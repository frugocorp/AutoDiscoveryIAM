// -----------------------------------------------------------------------
// <copyright file="TimeHelpers.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

/// <summary>
/// Helpers para manejo consistente de fechas y horarios en el proyecto.
/// Todo el proyecto utiliza horario de México (GMT-6).
/// </summary>
public static class TimeHelpers
{
  /// <summary>
  /// Obtiene la fecha y hora actual en horario de México (GMT-6).
  /// Este método garantiza consistencia en todo el proyecto.
  /// </summary>
  /// <returns>DateTime en horario de México.</returns>
  public static DateTime GetCurrentMexicoTime()
  {
    // NOTA: Este proyecto maneja fechas en zona horaria de México (GMT-6)
    // Si en el futuro se requiere soporte internacional, cambiar a UTC
    // y manejar conversiones en la capa de presentación
    var mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoTimeZone);
  }

  /// <summary>
  /// Convierte una fecha UTC a horario de México.
  /// </summary>
  /// <param name="utcDateTime">Fecha en UTC.</param>
  /// <returns>DateTime en horario de México.</returns>
  public static DateTime ConvertUtcToMexicoTime(DateTime utcDateTime)
  {
    var mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, mexicoTimeZone);
  }

  /// <summary>
  /// Convierte una fecha de horario de México a UTC.
  /// </summary>
  /// <param name="mexicoDateTime">Fecha en horario de México.</param>
  /// <returns>DateTime en UTC.</returns>
  public static DateTime ConvertMexicoTimeToUtc(DateTime mexicoDateTime)
  {
    var mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
    return TimeZoneInfo.ConvertTimeToUtc(mexicoDateTime, mexicoTimeZone);
  }
}

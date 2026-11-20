// -----------------------------------------------------------------------
// <copyright file="LogSanitizer.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <summary>
/// Neutraliza caracteres de control en textos que vienen del cliente (emails, rutas, detalles)
/// antes de escribirlos en el log.
/// </summary>
/// <remarks>
/// Un salto de línea en un email enviado al login permitiría inventar líneas completas en un log de
/// texto plano (por ejemplo, un "login exitoso" falso). Cada carácter de control se escribe escapado
/// (<c>\n</c>, <c>\r</c>, <c>\u001b</c>...), así el texto sigue siendo legible pero nunca rompe la línea.
/// </remarks>
internal static class LogSanitizer
{
  /// <summary>Devuelve <paramref name="value"/> con sus caracteres de control escapados.</summary>
  internal static string Clean(string value)
  {
    ArgumentNullException.ThrowIfNull(value);

    if (!value.Any(char.IsControl))
    {
      return value;
    }

    var builder = new StringBuilder(value.Length + 8);
    foreach (var character in value)
    {
      _ = character switch
      {
        '\n' => builder.Append(@"\n"),
        '\r' => builder.Append(@"\r"),
        '\t' => builder.Append(@"\t"),
        _ when char.IsControl(character) => builder.Append(@"\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture)),
        _ => builder.Append(character),
      };
    }

    return builder.ToString();
  }
}

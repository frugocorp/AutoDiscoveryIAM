// -----------------------------------------------------------------------
// <copyright file="ExternalProviderIdHasher.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <summary>
/// Hashea el identificador de sujeto (<c>sub</c>) que devuelve un proveedor de login social
/// (Google, etc.) antes de persistirlo en <see cref="Features.Auth.Models.Entities.ExternalLogin"/>.
/// </summary>
/// <remarks>
/// No es un secreto de acceso (a diferencia de una contraseña, no basta con conocer este valor
/// para iniciar sesión — se necesita una autenticación real y vigente con el proveedor), pero
/// tampoco hay razón para guardarlo en claro: hashearlo reduce el valor de la base de datos ante
/// una fuga sin perder la capacidad de hacer match exacto en el login (SHA-256 es determinista).
/// Solo se usa en el flujo de login/vinculación social — sin impacto en el
/// rendimiento de peticiones normales, ya que <c>ExternalLogin</c> nunca se consulta fuera de ese flujo.
/// </remarks>
public static class ExternalProviderIdHasher
{
  public static string Hash(string providerId)
  {
    var bytes = Encoding.UTF8.GetBytes(providerId);
    var hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash).ToLowerInvariant();
  }
}

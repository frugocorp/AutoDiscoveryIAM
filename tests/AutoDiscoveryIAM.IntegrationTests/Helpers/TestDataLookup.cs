// -----------------------------------------------------------------------
// <copyright file="TestDataLookup.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

/// <summary>
/// Resuelve el PublicId de usuarios, roles y permisos directo en la base de la prueba. Buscar en la
/// primera página de los listados (<c>/users?pageSize=100</c>) es frágil: la base de pruebas se
/// comparte entre clases (y, con SQL Server, entre corridas), y los registros que crean otras pruebas
/// empujan a los del seed fuera de esa página.
/// </summary>
public static class TestDataLookup
{
  public static Task<Guid> GetUserPublicIdByEmailAsync(IServiceProvider services, string email)
    => QueryAsync(services, db => db.Users.Where(u => u.Email == email).Select(u => u.PublicId), $"usuario con email {email}");

  public static Task<Guid> GetUserPublicIdByUsernameAsync(IServiceProvider services, string username)
    => QueryAsync(services, db => db.Users.Where(u => u.Username == username).Select(u => u.PublicId), $"usuario {username}");

  public static Task<Guid> GetRolePublicIdByNameAsync(IServiceProvider services, string name)
    => QueryAsync(services, db => db.Roles.Where(r => r.Name == name).Select(r => r.PublicId), $"rol {name}");

  public static Task<Guid> GetPermissionPublicIdByKeyAsync(IServiceProvider services, string key)
    => QueryAsync(services, db => db.Permissions.Where(p => p.Key == key).Select(p => p.PublicId), $"permiso {key}");

  private static async Task<Guid> QueryAsync(IServiceProvider services, Func<AutoDiscoveryIAMDbContext, IQueryable<Guid>> query, string description)
  {
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var publicIds = await query(db).ToListAsync(CancellationToken.None);
    return publicIds.Count == 1
      ? publicIds[0]
      : throw new InvalidOperationException($"Se esperaba exactamente un {description} en la base de pruebas y hay {publicIds.Count}.");
  }
}

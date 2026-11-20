// -----------------------------------------------------------------------
// <copyright file="AdminRoleCapabilitiesTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Authorization.RbacSecurity;

/// <summary>
/// Issue #103: un usuario SIN flags especiales, solo con el rol sembrado <c>AdminAutoDiscoveryIAM</c>,
/// administra la app desde el primer día (consulta usuarios y roles, asigna permisos y roles,
/// aprueba registros), pero sigue sin poder lo que está reservado a los flags que solo se marcan en
/// la base de datos (<c>IsSuAdmin</c>/<c>IsDevelopment</c>).
/// </summary>
[Collection("GlobalTestSetup")]
public class AdminRoleCapabilitiesTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string AdminRoleName = "AdminAutoDiscoveryIAM";
  private static readonly TestUser Manager = new("Manager", "manager@example.com", "ManagerPass123!", "Usuario sin flags al que se le asigna el rol administrador");

  [Fact]
  public async Task UserWithOnlyTheAdminRole_ShouldAdministerRolesAndUsers_ButNotFlagReservedOperations()
  {
    using var suAdmin = factory.CreateClient();
    await suAdmin.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);
    var rolePublicId = await FindPublicIdAsync(suAdmin, "/roles?pageSize=100", "name", AdminRoleName);
    var managerPublicId = await FindPublicIdAsync(suAdmin, "/users?pageSize=100", "email", Manager.Email!);
    var regularPublicId = await FindPublicIdAsync(suAdmin, "/users?pageSize=100", "email", "user@example.com");
    var suAdminPublicId = await FindPublicIdAsync(suAdmin, "/users?pageSize=100", "email", "su@example.com");

    var assign = await suAdmin.PutAsync($"/users/{managerPublicId}/roles/{rolePublicId}", content: null, CancellationToken.None);
    assign.StatusCode.Should().Be(HttpStatusCode.OK);

    try
    {
      using var manager = factory.CreateClient();
      await manager.AuthenticateUserAsync(Manager, CancellationToken.None);

      // Administra: antes, todo esto exigía IsSuAdmin y el rol no servía para nada.
      (await manager.GetAsync("/users", CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);
      (await manager.GetAsync("/roles", CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);
      (await manager.GetAsync("/permissions", CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);
      (await manager.GetAsync($"/roles/{rolePublicId}/permissions", CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);
      (await manager.GetAsync($"/users/{regularPublicId}/roles", CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);

      // Lo reservado a IsSuAdmin/IsDevelopment sigue fuera de su alcance.
      var promote = await manager.PutAsJsonAsync($"/users/{regularPublicId}/admin-type", new { isAdmin = true, reason = "prueba" }, CancellationToken.None);
      promote.StatusCode.Should().Be(HttpStatusCode.Forbidden);
      var sync = await manager.PostAsJsonAsync("/permissions/sync", new { forceFullSync = false, dryRun = true }, CancellationToken.None);
      sync.StatusCode.Should().Be(HttpStatusCode.Forbidden);

      // Tampoco puede tomar la cuenta IsSuAdmin cambiándole el email (para luego pedir el reseteo).
      var takeover = await manager.PutAsJsonAsync($"/users/{suAdminPublicId}", new { email = "attacker@example.com" }, CancellationToken.None);
      takeover.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    finally
    {
      await suAdmin.DeleteAsync($"/users/{managerPublicId}/roles/{rolePublicId}", CancellationToken.None);
    }
  }

  private static async Task<string> FindPublicIdAsync(HttpClient client, string url, string property, string value)
  {
    var json = await client.GetStringAsync(url, CancellationToken.None);
    using var document = JsonDocument.Parse(json);
    return FindPublicId(document.RootElement, property, value)
      ?? throw new InvalidOperationException($"No se encontró {property}={value} en {url}");
  }

  private static string? FindPublicId(JsonElement element, string property, string value)
  {
    if (element.ValueKind == JsonValueKind.Object)
    {
      if (element.TryGetProperty(property, out var candidate)
          && candidate.ValueKind == JsonValueKind.String
          && string.Equals(candidate.GetString(), value, StringComparison.OrdinalIgnoreCase)
          && element.TryGetProperty("publicId", out var publicId))
      {
        return publicId.GetString();
      }

      foreach (var child in element.EnumerateObject())
      {
        if (FindPublicId(child.Value, property, value) is { } found)
        {
          return found;
        }
      }
    }
    else if (element.ValueKind == JsonValueKind.Array)
    {
      foreach (var item in element.EnumerateArray())
      {
        if (FindPublicId(item, property, value) is { } found)
        {
          return found;
        }
      }
    }

    return null;
  }
}

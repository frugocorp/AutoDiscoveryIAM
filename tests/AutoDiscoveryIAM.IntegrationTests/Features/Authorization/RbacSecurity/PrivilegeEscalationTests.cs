// -----------------------------------------------------------------------
// <copyright file="PrivilegeEscalationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Authorization.RbacSecurity;

/// <summary>
/// Regla de no escalada: un usuario sin flags, con un rol que solo le permite asignar roles y editar
/// usuarios, no puede darse más permisos de los que tiene ni tomar la cuenta de quien tiene más.
/// </summary>
[Collection("GlobalTestSetup")]
public class PrivilegeEscalationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string AdminRoleName = "AdminAutoDiscoveryIAM";
  private static readonly TestUser Manager = new("Manager", "manager@example.com", "ManagerPass123!", "Usuario sin flags con un rol de soporte limitado");

  [Fact(DisplayName = "Con un rol limitado no se puede asignar un rol con más permisos, ni cambiar el email de quien tiene más")]
  public async Task LimitedRole_CannotEscalateOrTakeOverStrongerAccounts()
  {
    using var suAdmin = factory.CreateClient();
    await suAdmin.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var adminRolePublicId = await FindPublicIdAsync(suAdmin, "/roles?pageSize=100", "name", AdminRoleName);
    var managerPublicId = await FindPublicIdAsync(suAdmin, "/users?pageSize=100", "email", Manager.Email!);
    var regularPublicId = await FindPublicIdAsync(suAdmin, "/users?pageSize=100", "email", "user@example.com");
    var strongerPublicId = await FindPublicIdAsync(suAdmin, "/users?pageSize=100", "email", "test@example.com");
    var assignRolesPermission = await FindPublicIdAsync(suAdmin, "/permissions?pageSize=100", "key", "users.create_user_roles");
    var updateUserPermission = await FindPublicIdAsync(suAdmin, "/permissions?pageSize=100", "key", "users.update_user");

    var supportRole = await suAdmin.PostAsJsonAsync("/roles", new { name = $"test-role-escalation-{Guid.NewGuid():N}", description = "Soporte limitado", isActive = true }, CancellationToken.None);
    supportRole.StatusCode.Should().Be(HttpStatusCode.Created);
    var supportRolePublicId = await ReadPublicIdAsync(supportRole);

    (await suAdmin.PutAsync($"/roles/{supportRolePublicId}/permissions/{assignRolesPermission}", content: null, CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);
    (await suAdmin.PutAsync($"/roles/{supportRolePublicId}/permissions/{updateUserPermission}", content: null, CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);
    (await suAdmin.PutAsync($"/users/{managerPublicId}/roles/{supportRolePublicId}", content: null, CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);
    (await suAdmin.PutAsync($"/users/{strongerPublicId}/roles/{adminRolePublicId}", content: null, CancellationToken.None)).StatusCode.Should().Be(HttpStatusCode.OK);

    try
    {
      using var manager = factory.CreateClient();
      await manager.AuthenticateUserAsync(Manager, CancellationToken.None);

      // Darse a sí mismo el rol administrador: rechazado.
      var selfEscalation = await manager.PutAsync($"/users/{managerPublicId}/roles/{adminRolePublicId}", content: null, CancellationToken.None);
      selfEscalation.StatusCode.Should().Be(HttpStatusCode.Forbidden);

      // Cambiarle el email a quien tiene el rol administrador (para luego pedir el reseteo): rechazado.
      var takeover = await manager.PutAsJsonAsync($"/users/{strongerPublicId}", new { email = $"attacker-{Guid.NewGuid():N}@example.com" }, CancellationToken.None);
      takeover.StatusCode.Should().Be(HttpStatusCode.Forbidden);

      // Un rol cuyos permisos sí tiene, a un usuario con menos permisos: permitido.
      var allowed = await manager.PutAsync($"/users/{regularPublicId}/roles/{supportRolePublicId}", content: null, CancellationToken.None);
      allowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    finally
    {
      await suAdmin.DeleteAsync($"/users/{regularPublicId}/roles/{supportRolePublicId}", CancellationToken.None);
      await suAdmin.DeleteAsync($"/users/{managerPublicId}/roles/{supportRolePublicId}", CancellationToken.None);
      await suAdmin.DeleteAsync($"/users/{strongerPublicId}/roles/{adminRolePublicId}", CancellationToken.None);
      await suAdmin.DeleteAsync($"/roles/{supportRolePublicId}", CancellationToken.None);
    }
  }

  private static async Task<string> ReadPublicIdAsync(HttpResponseMessage response)
  {
    using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None));
    return document.RootElement.GetProperty("data").GetProperty("publicId").GetString()!;
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

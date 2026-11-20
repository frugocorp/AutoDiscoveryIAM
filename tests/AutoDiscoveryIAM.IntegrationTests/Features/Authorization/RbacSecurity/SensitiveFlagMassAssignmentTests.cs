// -----------------------------------------------------------------------
// <copyright file="SensitiveFlagMassAssignmentTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Authorization.RbacSecurity;

/// <summary>
/// Guarda de regresión: <c>User.IsAdmin</c>/<c>IsDevelopment</c>/<c>IsSuAdmin</c> saltan todo el
/// RBAC (ver <c>AuthorizationMiddleware.HandleSpecialUserAccessAsync</c>), así que
/// son el único punto del sistema sin una segunda capa de defensa detrás. Ningún DTO de request los
/// expone hoy, pero nada impide que un cambio futuro (un endpoint nuevo, un DTO "mejorado") los
/// termine leyendo del body por descuido. Estas pruebas fallan ruidosamente si eso llega a pasar.
/// </summary>
[Collection("GlobalTestSetup")]
public class SensitiveFlagMassAssignmentTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithExtraSensitiveFlagsInBody_ShouldIgnoreThem()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var maliciousPayload = new
        {
            Username = $"canary_{suffix}",
            Email = $"canary_{suffix}@example.com",
            Password = "CanaryPass123!",
            PhoneNumber = (string?)null,
            IsAdmin = true,
            IsDevelopment = true,
            IsSuAdmin = true,
        };

        var registerResponse = await _client.PostAsJsonAsync("/users", maliciousPayload, CancellationToken.None);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<CreateUserResponseDto>>(cancellationToken: CancellationToken.None);
        var publicId = registerPayload!.Data!.PublicId;

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);
        var getResponse = await _client.GetAsync($"/users/{publicId}", CancellationToken.None);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getPayload = await getResponse.Content.ReadFromJsonAsync<ApiResponse<GetUserByPublicIdResponseDto>>(cancellationToken: CancellationToken.None);

        getPayload!.Data!.IsAdmin.Should().BeFalse("CreateUserRequestDto no expone este campo -- el body extra debe ser ignorado");
        getPayload.Data.IsDevelopment.Should().BeFalse("CreateUserRequestDto no expone este campo -- el body extra debe ser ignorado");
        getPayload.Data.IsSuAdmin.Should().BeFalse("CreateUserRequestDto no expone este campo -- el body extra debe ser ignorado");
    }

    [Fact]
    public async Task UpdateMyProfile_WithExtraSensitiveFlagsInBody_ShouldIgnoreThem()
    {
        var authenticated = await _client.AuthenticateUserAsync(UserType.Regular, CancellationToken.None);

        var maliciousPayload = new
        {
            PhoneNumber = "+521234567890",
            IsAdmin = true,
            IsDevelopment = true,
            IsSuAdmin = true,
        };

        var updateResponse = await _client.PutAsJsonAsync("/users/me", maliciousPayload, CancellationToken.None);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var regularUser = TestUsersHelper.GetUserByType(UserType.Regular);
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);
        // pageSize al maximo permitido (PaginationDefaults.MaxPageSize) a proposito: con la base de
        // test real, otros tests dejan usuarios desechables entre corridas (limpiados best-effort)
        // que pueden empujar a "user@example.com" fuera de la primera pagina por defecto -- este
        // test no depende de que esa limpieza sea perfecta.
        var usersResponse = await _client.GetAsync("/users?pageNumber=1&pageSize=100", CancellationToken.None);
        var usersPayload = await usersResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<GetUsersResponseDto>>>(cancellationToken: CancellationToken.None);

        var updatedUser = usersPayload!.Data!.Items.Single(u => u.Email == regularUser.Email);

        updatedUser.IsAdmin.Should().BeFalse("UpdateMyProfileRequestDto no expone este campo -- el body extra debe ser ignorado");
        updatedUser.IsDevelopment.Should().BeFalse("UpdateMyProfileRequestDto no expone este campo -- el body extra debe ser ignorado");
        updatedUser.IsSuAdmin.Should().BeFalse("UpdateMyProfileRequestDto no expone este campo -- el body extra debe ser ignorado");
    }

    [Trait("Category", "Security")]
    [Fact(DisplayName = "UpdateUser (administrado por Admin sobre otro usuario) ignora flags sensibles extra en el body")]
    public async Task UpdateUser_ByAdmin_WithExtraSensitiveFlagsInBody_ShouldIgnoreThem()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var registerPayload = new
        {
            Username = $"canary_admin_{suffix}",
            Email = $"canary_admin_{suffix}@example.com",
            Password = "CanaryPass123!",
            PhoneNumber = (string?)null,
        };

        var registerResponse = await _client.PostAsJsonAsync("/users", registerPayload, CancellationToken.None);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerContent = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<CreateUserResponseDto>>(cancellationToken: CancellationToken.None);
        var targetPublicId = registerContent!.Data!.PublicId;

        await _client.AuthenticateUserAsync(UserType.Admin, CancellationToken.None);

        var maliciousPayload = new
        {
            Username = (string?)null,
            Email = (string?)null,
            Notes = "actualizado por admin",
            IsAdmin = true,
            IsDevelopment = true,
            IsSuAdmin = true,
        };

        var updateResponse = await _client.PutAsJsonAsync($"/users/{targetPublicId}", maliciousPayload, CancellationToken.None);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/users/{targetPublicId}", CancellationToken.None);
        var getPayload = await getResponse.Content.ReadFromJsonAsync<ApiResponse<GetUserByPublicIdResponseDto>>(cancellationToken: CancellationToken.None);

        getPayload!.Data!.IsAdmin.Should().BeFalse("UpdateUserBodyDto no expone este campo -- el body extra debe ser ignorado");
        getPayload.Data.IsDevelopment.Should().BeFalse("UpdateUserBodyDto no expone este campo -- el body extra debe ser ignorado");
        getPayload.Data.IsSuAdmin.Should().BeFalse("UpdateUserBodyDto no expone este campo -- el body extra debe ser ignorado");
    }
}

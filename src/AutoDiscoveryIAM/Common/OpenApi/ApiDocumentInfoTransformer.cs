// -----------------------------------------------------------------------
// <copyright file="ApiDocumentInfoTransformer.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

/// <summary>
/// Declara la metadata general del documento (título, versión, contacto) y el esquema de seguridad
/// Bearer (JWT), a nivel de documento.
/// </summary>
/// <remarks>
/// Se aplica vía <c>IOpenApiDocumentTransformer</c>, nativo de <c>Microsoft.AspNetCore.OpenApi</c>,
/// registrado una sola vez en <c>AddOpenApi(...)</c>.
/// </remarks>
public sealed class ApiDocumentInfoTransformer : IOpenApiDocumentTransformer
{
  public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
  {
    document.Info = new OpenApiInfo
    {
      Title = "AutoDiscoveryIAM API",
      Version = "v1",
      Description = "API REST con autenticación JWT y RBAC dinámico (roles y permisos descubiertos automáticamente)",
      Contact = new OpenApiContact
      {
        Name = "FrugoCorp",
        Email = "contacto@autodiscoveryiam.com",
      },
    };

    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
      Description = "Cabecera de autorización JWT usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
      Name = "Authorization",
      In = ParameterLocation.Header,
      Type = SecuritySchemeType.Http,
      Scheme = "bearer",
      BearerFormat = "JWT",
    };

    document.Security ??= new List<OpenApiSecurityRequirement>();
    document.Security.Add(new OpenApiSecurityRequirement
    {
      [new OpenApiSecuritySchemeReference("Bearer", document, null)] = [],
    });

    return Task.CompletedTask;
  }
}

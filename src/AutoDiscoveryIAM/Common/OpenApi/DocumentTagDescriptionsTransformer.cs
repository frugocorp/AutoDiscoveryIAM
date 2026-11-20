// -----------------------------------------------------------------------
// <copyright file="DocumentTagDescriptionsTransformer.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

/// <summary>
/// Declara una sola vez, a nivel de documento, las descripciones de todos los tags conocidos
/// (<see cref="OpenApiTagDescriptions"/>). En Microsoft.OpenApi 2.x cada operación referencia un
/// tag por nombre (<c>OpenApiTagReference</c>) contra esta lista -- no hace falta (ni se puede,
/// sin acceso al <c>OpenApiDocument</c>) repetir la descripción en cada endpoint.
/// </summary>
/// <remarks>
/// Reemplaza al antiguo <c>DocumentTagDescriptionsFilter : IDocumentFilter</c> (Swashbuckle) -- ver
/// el generador del documento OpenAPI pasa a ser el nativo de
/// <c>Microsoft.AspNetCore.OpenApi</c>, así que el gancho es <c>IOpenApiDocumentTransformer</c>.
/// </remarks>
public sealed class DocumentTagDescriptionsTransformer : IOpenApiDocumentTransformer
{
  public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
  {
    document.Tags = OpenApiTagDescriptions.All();

    return Task.CompletedTask;
  }
}

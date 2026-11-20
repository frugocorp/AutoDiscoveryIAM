// -----------------------------------------------------------------------
// <copyright file="EndpointOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

/// <summary>
/// Unico punto que toca tipos de <c>Microsoft.OpenApi.*</c> para documentar operaciones. Las
/// subclases por slice (una por endpoint, en su propia carpeta <c>Docs/</c>) exponen solo datos
/// simples -- nunca tipos de <c>Microsoft.OpenApi.*</c> ni de ninguna herramienta de renderizado --
/// para que un futuro cambio de versión mayor de Microsoft.OpenApi, o de la herramienta de
/// documentación elegida (Swagger UI, Scalar, ReDoc, Mintlify, ninguna), solo requiera tocar esta
/// clase, nunca los endpoints.
/// </summary>
/// <remarks>
/// Se aplica vía <see cref="EndpointOperationDocsTransformer"/> (un <c>IOpenApiOperationTransformer</c>
/// **nativo** de <c>Microsoft.AspNetCore.OpenApi</c>, no de Swashbuckle) que busca la instancia en los
/// metadatos del endpoint -- reemplaza a <c>.WithOpenApi(...)</c>, deprecado (ASPDEPR002) desde .NET
/// 10. Cada endpoint adjunta su propia subclase vía <c>.WithMetadata(new XxxOperationDocs())</c>.
/// <para>
/// Los ejemplos de error se toman de <see cref="ErrorInfo.ToExampleResponse"/> -- una sola fuente
/// de verdad por código de estado, en vez de una clase <c>IExamplesProvider</c> dedicada por
/// endpoint.
/// </para>
/// <para>
/// Aplicada vía <c>IOpenApiOperationTransformer</c> nativo de <c>Microsoft.AspNetCore.OpenApi</c>,
/// no un <c>IOperationFilter</c> de Swashbuckle -- así el generador del documento OpenAPI (y por lo
/// tanto la herramienta de renderizado) es una elección libre de quien instale el paquete, sin tocar
/// esta clase ni los endpoints.
/// </para>
/// </remarks>
public abstract class EndpointOperationDocs
{
  /// <summary>
  /// Código de estado de la respuesta exitosa principal de este endpoint (200, 201, etc.).
  /// </summary>
  protected abstract int SuccessStatusCode { get; }

  /// <summary>
  /// Descripción custom de la respuesta exitosa, si aplica (por defecto se deja la que el generador
  /// de OpenAPI produce automáticamente).
  /// </summary>
  protected virtual string? SuccessDescription => null;

  /// <summary>
  /// Ejemplo de la respuesta exitosa (precarga el bloque "Example" en "Try it out"). Reemplaza al
  /// antiguo <c>SwaggerResponseExampleAttribute</c> (<c>Swashbuckle.AspNetCore.Filters</c>, sin
  /// equivalente nativo).
  /// </summary>
  protected virtual object? SuccessExample => null;

  /// <summary>
  /// Códigos de error que aplican a este endpoint, mapeados a su <see cref="ErrorInfo"/> canónico.
  /// </summary>
  protected abstract IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; }

  /// <summary>
  /// Link opcional de la respuesta exitosa hacia otra operación (ej. "GetRoleByPublicId" tras un
  /// <c>CreateRole</c>). Solo aplica a un puñado de endpoints que crean/actualizan un recurso.
  /// </summary>
  protected virtual SuccessLinkInfo? SuccessLink => null;

  /// <summary>
  /// Descripción custom del <c>RequestBody</c>, para los endpoints que necesitan aclarar algo
  /// (ej. una bandera DryRun) más allá del schema generado automáticamente.
  /// </summary>
  protected virtual string? RequestBodyDescription => null;

  /// <summary>
  /// Ejemplo del <c>RequestBody</c> (precarga el JSON en "Try it out"). Reemplaza al antiguo
  /// <c>SwaggerRequestExampleAttribute</c> (<c>Swashbuckle.AspNetCore.Filters</c>, sin equivalente
  /// nativo).
  /// </summary>
  protected virtual object? RequestExample => null;

  /// <summary>
  /// Descripción/ejemplo por parámetro de ruta (ej. <c>rolePublicId</c>), mapeados por nombre.
  /// Solo aplica a endpoints con parámetros que merecen aclaración más allá del schema generado
  /// automáticamente (típicamente PublicIds en rutas con dos o más segmentos <c>:guid</c>).
  /// </summary>
  protected virtual IReadOnlyDictionary<string, ParameterDocInfo>? Parameters => null;

  public void Apply(OpenApiOperation operation)
  {
    ApplySuccessResponse(operation);
    ApplyErrorResponses(operation);
    ApplyRequestBody(operation);
    ApplyParameters(operation);
  }

  private void ApplySuccessResponse(OpenApiOperation operation)
  {
    if (operation.Responses is null)
    {
      return;
    }

    if (!operation.Responses.TryGetValue(SuccessStatusCode.ToString(CultureInfo.InvariantCulture), out var successResponse) ||
        successResponse is not OpenApiResponse concreteSuccessResponse)
    {
      return;
    }

    if (SuccessDescription is { } successDescription)
    {
      concreteSuccessResponse.Description = successDescription;
    }

    if (SuccessExample is { } successExample)
    {
      concreteSuccessResponse.Content ??= new Dictionary<string, OpenApiMediaType>();
      SetExample(concreteSuccessResponse.Content, successExample);
    }

    if (SuccessLink is { } link)
    {
      concreteSuccessResponse.Links ??= new Dictionary<string, IOpenApiLink>();
      concreteSuccessResponse.Links[link.LinkedOperationId] = new OpenApiLink
      {
        OperationId = link.LinkedOperationId,
        Description = link.Description,
        Parameters = new Dictionary<string, RuntimeExpressionAnyWrapper>
        {
          [link.ParameterName] = new RuntimeExpressionAnyWrapper { Expression = RuntimeExpression.Build(link.Expression) },
        },
      };
    }
  }

  private void ApplyErrorResponses(OpenApiOperation operation)
  {
    if (operation.Responses is null)
    {
      return;
    }

    foreach (var (statusCode, errorInfo) in ErrorResponses)
    {
      if (operation.Responses.TryGetValue(statusCode.ToString(CultureInfo.InvariantCulture), out var response) &&
          response is OpenApiResponse concreteResponse)
      {
        concreteResponse.Description = errorInfo.Description;
        concreteResponse.Content ??= new Dictionary<string, OpenApiMediaType>();
        SetExample(concreteResponse.Content, errorInfo.ToExampleResponse());
      }
    }
  }

  private void ApplyRequestBody(OpenApiOperation operation)
  {
    if (operation.RequestBody is not OpenApiRequestBody concreteRequestBody)
    {
      return;
    }

    if (RequestBodyDescription is { } requestBodyDescription)
    {
      concreteRequestBody.Description = requestBodyDescription;
    }

    if (RequestExample is { } requestExample)
    {
      concreteRequestBody.Content ??= new Dictionary<string, OpenApiMediaType>();
      SetExample(concreteRequestBody.Content, requestExample);
    }
  }

  private void ApplyParameters(OpenApiOperation operation)
  {
    if (Parameters is not { } parameterDocs || operation.Parameters is null)
    {
      return;
    }

    foreach (var parameter in operation.Parameters)
    {
      if (parameter is OpenApiParameter concreteParameter &&
          concreteParameter.Name is { } parameterName &&
          parameterDocs.TryGetValue(parameterName, out var doc))
      {
        concreteParameter.Description = doc.Description;
        if (doc.Example is { } example)
        {
          concreteParameter.Example = JsonSerializer.SerializeToNode(example);
        }
      }
    }
  }

  private static void SetExample(IDictionary<string, OpenApiMediaType> content, object example)
  {
    if (!content.TryGetValue("application/json", out var mediaType))
    {
      mediaType = new OpenApiMediaType();
      content["application/json"] = mediaType;
    }

    mediaType.Example = JsonSerializer.SerializeToNode(example);
  }
}

/// <summary>
/// Datos simples para documentar un <see cref="Microsoft.OpenApi.OpenApiLink"/> en la respuesta
/// exitosa de un endpoint (ej. enlazar el 201 de <c>CreateRole</c> a <c>GetRoleByPublicId</c>).
/// </summary>
public sealed record SuccessLinkInfo(string LinkedOperationId, string Description, string ParameterName, string Expression);

/// <summary>
/// Descripción/ejemplo de un parámetro de ruta específico (ver <see cref="EndpointOperationDocs.Parameters"/>).
/// </summary>
public sealed record ParameterDocInfo(string Description, string? Example = null);

/// <summary>
/// Transformer global nativo: busca en los metadatos del endpoint una instancia
/// de <see cref="EndpointOperationDocs"/> -- adjunta vía <c>.WithMetadata(new XxxOperationDocs())</c>
/// -- y la aplica. Un único registro (<c>options.AddOperationTransformer&lt;EndpointOperationDocsTransformer&gt;()</c>
/// dentro de <c>AddOpenApi(...)</c> en <c>Program.cs</c>) cubre todos los endpoints migrados, sin
/// opt-in adicional por endpoint más allá de adjuntar su propia subclase.
/// </summary>
/// <remarks>
/// Reemplaza al antiguo <c>EndpointOperationDocsFilter : IOperationFilter</c> (Swashbuckle) -- el
/// nuevo gancho es <c>IOpenApiOperationTransformer</c>, nativo de <c>Microsoft.AspNetCore.OpenApi</c>,
/// para que el generador del documento (y por lo tanto la herramienta de renderizado) no esté atado a
/// Swashbuckle.
/// </remarks>
public sealed class EndpointOperationDocsTransformer : IOpenApiOperationTransformer
{
  public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
  {
    var docs = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointOperationDocs>()
      .FirstOrDefault();

    docs?.Apply(operation);

    return Task.CompletedTask;
  }
}

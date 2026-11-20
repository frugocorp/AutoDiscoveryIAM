// -----------------------------------------------------------------------
// <copyright file="ApiResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

/// <summary>
/// Estructura estándar para todas las respuestas de la API.
/// </summary>
/// <typeparam name="T">Tipo del dato devuelto en la respuesta.</typeparam>
/// <param name="Success">Indica si la operación fue exitosa.</param>
/// <param name="Data">Dato devuelto por la operación, o <c>null</c> si falla.</param>
/// <param name="Error">Mensaje de error, o <c>null</c> si la operación fue exitosa.</param>
/// <param name="ErrorInfo">Objeto con metadatos del error, o <c>null</c> si la operación fue exitosa.</param>
public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error = null,
    ErrorInfo? ErrorInfo = null);

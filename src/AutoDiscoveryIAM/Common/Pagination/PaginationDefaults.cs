// -----------------------------------------------------------------------
// <copyright file="PaginationDefaults.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

/// <summary>
/// Constantes y helper de paginación compartidos por todos los endpoints de listado.
/// </summary>
public static class PaginationDefaults
{
  public const int DefaultPageNumber = 1;
  public const int DefaultPageSize = 20;
  public const int MaxPageSize = 100;

  /// <summary>Página máxima: con <see cref="MaxPageSize"/>, el desplazamiento sigue cabiendo en un int.</summary>
  public const int MaxPageNumber = int.MaxValue / MaxPageSize;

  /// <summary>
  /// Máximo de identificadores permitidos en una sola operación bulk.
  /// </summary>
  public const int MaxBulkAssignmentSize = 100;

  /// <summary>
  /// Ajusta silenciosamente los parámetros de paginación a un rango válido, sin exigir un error 400
  /// por un valor fuera de rango (ej. pageSize=0 o pageSize=99999).
  /// </summary>
  public static (int PageNumber, int PageSize) Clamp(int pageNumber, int pageSize)
  {
    // Tope: (pageNumber - 1) * pageSize no puede desbordar int (un OFFSET negativo haría fallar la consulta con 500).
    var clampedPageNumber = pageNumber < 1 ? DefaultPageNumber : Math.Min(pageNumber, MaxPageNumber);
    var clampedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    return (clampedPageNumber, clampedPageSize);
  }
}

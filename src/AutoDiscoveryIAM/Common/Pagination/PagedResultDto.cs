// -----------------------------------------------------------------------
// <copyright file="PagedResultDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

/// <summary>
/// Envoltura estándar para respuestas paginadas de endpoints de listado.
/// </summary>
public record PagedResultDto<T>(
    IEnumerable<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
  public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

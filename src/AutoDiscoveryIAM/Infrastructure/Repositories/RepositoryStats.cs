// -----------------------------------------------------------------------
// <copyright file="RepositoryStats.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;

/// <summary>
/// Estadísticas de operaciones realizadas por un repositorio.
/// </summary>
public class RepositoryStats
{
    /// <summary>
    /// Número de entidades insertadas.
    /// </summary>
    public int Inserted { get; internal set; }

    /// <summary>
    /// Número de entidades actualizadas.
    /// </summary>
    public int Updated { get; internal set; }

    /// <summary>
    /// Número de entidades eliminadas.
    /// </summary>
    public int Deleted { get; internal set; }

    /// <summary>
    /// Número de entidades restauradas.
    /// </summary>
    public int Restored { get; internal set; }

    /// <summary>
    /// Número de entidades bloqueadas.
    /// </summary>
    public int Blocked { get; internal set; }

    /// <summary>
    /// Número de consultas ejecutadas.
    /// </summary>
    public int QueriesExecuted { get; internal set; }

    /// <summary>
    /// Reinicia todas las estadísticas a cero.
    /// </summary>
    public void Reset()
    {
        Inserted = 0;
        Updated = 0;
        Deleted = 0;
        Restored = 0;
        Blocked = 0;
        QueriesExecuted = 0;
    }
}

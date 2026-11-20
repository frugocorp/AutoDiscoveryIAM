// -----------------------------------------------------------------------
// <copyright file="BaseRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;

/// <summary>
/// Implementación base genérica para operaciones CRUD estándar.
/// </summary>
/// <typeparam name="T">Tipo de entidad a manejar.</typeparam>
public abstract class BaseRepository<T>(AutoDiscoveryIAMDbContext context) : IBaseRepository<T>
    where T : class
{
    /// <summary>
    /// Contexto de base de datos para acceso a entidades.
    /// </summary>
    protected AutoDiscoveryIAMDbContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// Estadísticas de operaciones realizadas por este repositorio.
    /// </summary>
    public RepositoryStats Stats { get; } = new();

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        return await Context.Set<T>().FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        return await Context.Set<T>().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual T Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entityEntry = Context.Set<T>().Add(entity);
        Stats.Inserted++;
        return entityEntry.Entity;
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entityEntry = await Context.Set<T>().AddAsync(entity, cancellationToken);
        Stats.Inserted++;
        return entityEntry.Entity;
    }

    /// <inheritdoc />
    public virtual T Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entityEntry = Context.Set<T>().Update(entity);
        Stats.Updated++;
        return entityEntry.Entity;
    }

    /// <inheritdoc />
    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entityEntry = Context.Set<T>().Update(entity);
        Stats.Updated++;
        await Task.Yield(); // Para mantener la firma async
        return entityEntry.Entity;
        // return Task.FromResult(entityEntry.Entity).Result;
    }

    /// <inheritdoc />
    public virtual async Task<T> RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entityEntry = Context.Set<T>().Remove(entity);
        Stats.Deleted++;
        await Task.Yield(); // Para mantener la firma async
        return entityEntry.Entity;
    }

    /// <inheritdoc />
    public virtual T Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entityEntry = Context.Set<T>().Remove(entity);
        Stats.Deleted++;
        return entityEntry.Entity;
    }

    /// <inheritdoc />
    public virtual async Task<bool> RemoveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            Context.Set<T>().Remove(entity);
            Stats.Deleted++;
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        var entity = await Context.Set<T>().FindAsync([id], cancellationToken);
        return entity != null;
    }
}

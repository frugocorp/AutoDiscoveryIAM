// -----------------------------------------------------------------------
// <copyright file="IBaseRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;

/// <summary>
/// Interfaz base genérica para operaciones CRUD estándar.
/// </summary>
/// <typeparam name="T">Tipo de entidad a manejar.</typeparam>
public interface IBaseRepository<T> where T : class
{
    /// <summary>
    /// Obtiene una entidad por su ID.
    /// </summary>
    /// <param name="id">ID de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Entidad encontrada o null si no existe.</returns>
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todas las entidades.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de todas las entidades.</returns>
    Task<IList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega una nueva entidad.
    /// </summary>
    /// <param name="entity">Entidad a agregar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La entidad creada con los campos generados por la base de datos.</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una entidad existente.
    /// </summary>
    /// <param name="entity">Entidad a actualizar.</param>
    /// <returns>La entidad actualizada.</returns>
    T Update(T entity);

    /// <summary>
    /// Actualiza una entidad existente.
    /// </summary>
    /// <param name="entity">Entidad a actualizar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La entidad actualizada.</returns>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una entidad directamente.
    /// </summary>
    /// <param name="entity">Entidad a actualizar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La entidad actualizada.</returns>
    Task<T> RemoveAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una entidad directamente.
    /// </summary>
    /// <param name="entity">Entidad a eliminar.</param>
    /// <returns>La entidad eliminada.</returns>
    T Remove(T entity);

    /// <summary>
    /// Verifica si existe una entidad con el ID especificado.
    /// </summary>
    /// <param name="id">ID de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>True si existe, false en caso contrario.</returns>
    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
}

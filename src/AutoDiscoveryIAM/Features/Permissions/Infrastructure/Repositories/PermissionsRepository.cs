// -----------------------------------------------------------------------
// <copyright file="PermissionsRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio para operaciones específicas de la feature Permissions.
/// </summary>
public class PermissionsRepository(AutoDiscoveryIAMDbContext context)
    : BaseRepository<Permission>(context), IPermissionsRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// Ignora el filtro global de soft-delete (<c>IgnoreQueryFilters</c>) a propósito: el llamador
    /// (<c>SyncPermissionsService.ProcessNewPermissionsAsync</c>) necesita encontrar permisos ya
    /// existentes aunque estén marcados <c>IsDeleted</c>/<c>IsActive=false</c>, para reactivarlos en
    /// vez de intentar insertar un duplicado que choque con el índice único de <c>Key</c>.
    /// </remarks>
    public virtual async Task<Permission?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Stats.QueriesExecuted++;

        return await Context.Permissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Key == key, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IList<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        return await Context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Permission>> GetByGroupAsync(string group, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        Stats.QueriesExecuted++;

        return await Context.Permissions
            .Where(p => p.Group == group)
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Permission>> GetSystemGeneratedPermissionsAsync(CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        return await Context.Permissions
            .Where(p => p.IsSystemGenerated)
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Permission>> SearchPermissionsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);
        Stats.QueriesExecuted++;

        return await Context.Permissions
            .Where(p =>
                p.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (p.Group != null && p.Group.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Permission>> GetPermissionsNotRequiredAsync(CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        return await Context.Permissions
            .Where(p => p.HasPermissionsNotRequired)
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Permission>> GetDevelopmentAccessPermissionsAsync(CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        return await Context.Permissions
            .Where(p => p.RequiresDevelopmentAccess)
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Permission>> GetSuAdminAccessPermissionsAsync(CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        return await Context.Permissions
            .Where(p => p.RequiresSuAdminAccess)
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Permission>> GetExplicitPermissionValidationAsync(CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;
        return await Context.Permissions
            .Where(p => p.HasRequirePermissions)
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Permission>> GetPermissionsByAuthorizationTypeAsync(
        bool permissionsNotRequired = false,
        bool developmentAccess = false,
        bool suAdminAccess = false,
        bool explicitValidation = false,
        CancellationToken cancellationToken = default)
    {
        Stats.QueriesExecuted++;

        var query = Context.Permissions.AsQueryable();

        // Construir filtros dinámicos basados en los parámetros
        var filters = new List<System.Linq.Expressions.Expression<Func<Permission, bool>>>();

        if (permissionsNotRequired)
            filters.Add(p => p.HasPermissionsNotRequired);

        if (developmentAccess)
            filters.Add(p => p.RequiresDevelopmentAccess);

        if (suAdminAccess)
            filters.Add(p => p.RequiresSuAdminAccess);

        if (explicitValidation)
            filters.Add(p => p.HasRequirePermissions);

        // Si no hay filtros especificados, devolver lista vacía
        if (filters.Count == 0)
            return new List<Permission>();

        // Combinar filtros con OR
        var combinedFilter = filters.Aggregate((acc, next) =>
        {
            var param = System.Linq.Expressions.Expression.Parameter(typeof(Permission), "p");
            var left = System.Linq.Expressions.Expression.Invoke(acc, param);
            var right = System.Linq.Expressions.Expression.Invoke(next, param);
            var body = System.Linq.Expressions.Expression.OrElse(left, right);
            return System.Linq.Expressions.Expression.Lambda<Func<Permission, bool>>(body, param);
        });

        return await query
            .Where(combinedFilter)
            .OrderBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }
}

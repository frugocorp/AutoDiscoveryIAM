// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMDbContext.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Auditing;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Upgrades;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;

/// <summary>
/// DbContext del motor RBAC. No conoce proveedor de base de datos ni fuente de configuración
/// alguna -- ambos los decide exclusivamente quien registra <c>AddDbContext&lt;AutoDiscoveryIAMDbContext&gt;(options => ...)</c>,
/// para que un proyecto consumidor pueda elegir su propio proveedor de base de datos y su propia
/// fuente de configuración. La librería no trae migraciones empaquetadas ni una fábrica de diseño
/// propia -- cada proyecto consumidor genera las suyas con <c>dotnet ef migrations add</c> desde su
/// propio proyecto (ver README.md), igual que ya se necesita para agregar sus propias
/// entidades a la misma base de datos.
/// </summary>
public class AutoDiscoveryIAMDbContext : DbContext
{
  private readonly ICurrentAuditUserService? _currentAuditUserService;
  private readonly AuditableEntitySaveChangesInterceptor _auditInterceptor;
  private readonly PrivilegeEscalationSaveChangesInterceptor _privilegeEscalationInterceptor;

  // DbContextOptions (no el genérico DbContextOptions<AutoDiscoveryIAMDbContext>) a propósito:
  // es lo que permite que un consumidor herede esta clase en su propio DbContext (mismo patrón
  // que IdentityDbContext<TUser>) para fusionar ambos modelos en un solo contexto, sin obligarlo
  // a hacerlo -- ver README.md, "Elige tu propio DbContext: uno o dos".
  public AutoDiscoveryIAMDbContext(DbContextOptions options, ICurrentAuditUserService? currentAuditUserService = null) : base(options)
  {
    _currentAuditUserService = currentAuditUserService;
    _auditInterceptor = new AuditableEntitySaveChangesInterceptor(ResolveCurrentAuditUserId);
    _privilegeEscalationInterceptor = new PrivilegeEscalationSaveChangesInterceptor(() => _currentAuditUserService?.GetCurrentUser()?.PublicId);
  }

  public DbSet<User> Users { get; set; }
  public DbSet<Role> Roles { get; set; }
  public DbSet<Permission> Permissions { get; set; }
  public DbSet<UserRole> UserRoles { get; set; }
  public DbSet<RolePermission> RolePermissions { get; set; }
  public DbSet<RefreshToken> RefreshTokens { get; set; }
  public DbSet<ExternalLogin> ExternalLogins { get; set; }
  public DbSet<EmailChangeRequest> EmailChangeRequests { get; set; }
  public DbSet<ExternalLoginExchangeCode> ExternalLoginExchangeCodes { get; set; }

  /// <summary>
  /// Gets or sets los pasos de actualización de datos del paquete ya aplicados a esta base (ver
  /// <see cref="AutoDiscoveryIAMUpgradeRunner"/>).
  /// </summary>
  public DbSet<AutoDiscoveryIAMUpgradeRecord> Upgrades { get; set; }

  /// <summary>
  /// Nombre de usuario reservado para el usuario de sistema: existe siempre para
  /// integridad referencial de auditoría (<see cref="ResolveCurrentAuditUserId"/> lo usa como
  /// último fallback), pero <c>IsActive=false</c> impide que se use para iniciar sesión real.
  /// A diferencia del diseño anterior, no depende de que su <c>Id</c> sea exactamente 1 -- se
  /// resuelve por nombre, para no requerir un truco de IDENTITY_INSERT específico de SQL Server
  /// al sembrarlo (ver <see cref="Seeds.UserSeeder"/>).
  /// </summary>
  public const string SystemUsername = "system";

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    base.OnConfiguring(optionsBuilder);

    // Registra el interceptor de auditoría (Data/Auditing/AuditableEntitySaveChangesInterceptor.cs)
    // via el mecanismo nativo de EF Core -- no requiere que quien configura AddDbContext(...) en su
    // Program.cs sepa nada de auditoría. El mismo interceptor es público y reutilizable por un
    // DbContext propio de un consumidor (ver README.md, sección "Reutilizar auditoría/soft-delete").
    optionsBuilder.AddInterceptors(_auditInterceptor);

    // Regla de no escalada (Data/Authorization/PrivilegeEscalationSaveChangesInterceptor.cs): nadie
    // otorga ni administra, desde ningún endpoint, permisos que no tiene.
    optionsBuilder.AddInterceptors(_privilegeEscalationInterceptor);
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutoDiscoveryIAMDbContext).Assembly);

    // Los datos semilla (roles, permisos, usuarios) no viven en migraciones: los crea
    // SeedAutoDiscoveryIAMDefaultDataAsync() en tiempo de ejecución, sea cual sea el proveedor.
  }

  private long? ResolveCurrentAuditUserId()
  {
    var currentUser = _currentAuditUserService?.GetCurrentUser();

    if (currentUser?.Id is long id && id > 0)
    {
      return id;
    }

    if (currentUser?.PublicId is Guid publicId)
    {
      var resolvedId = Users
        .AsNoTracking()
        .Where(user => user.PublicId == publicId && !user.IsDeleted)
        .Select(user => user.Id)
        .FirstOrDefault();

      if (resolvedId > 0)
      {
        return resolvedId;
      }
    }

    return Users
      .AsNoTracking()
      .Where(user => user.Username == SystemUsername)
      .Select(user => user.Id)
      .FirstOrDefault();
  }
}

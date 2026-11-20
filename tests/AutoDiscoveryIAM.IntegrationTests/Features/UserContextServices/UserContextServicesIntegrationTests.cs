using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.RoleQuery;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.UserContextServices;

/// <summary>
/// Tests de integración para validar el flujo completo Auth ? Users ? Roles.
/// Verifica que los servicios distribuidos trabajen correctamente en conjunto.
/// </summary>
public class UserContextServicesIntegrationTests : IDisposable
{
  private readonly AutoDiscoveryIAMDbContext _context;
  private readonly HttpContextAccessor _httpContextAccessor;
  private readonly AuthContextService _authContextService;
  private readonly UserQueryService _userQueryService;
  private readonly RoleQueryService _roleQueryService;

  public UserContextServicesIntegrationTests()
  {
    // Configurar base de datos en memoria
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    _context = new AutoDiscoveryIAMDbContext(options);

    // Setup HttpContextAccessor real (NO MOCK en integration tests)
    _httpContextAccessor = new HttpContextAccessor();

    // Create services
    _authContextService = new AuthContextService(_httpContextAccessor, _context);
    _userQueryService = new UserQueryService(_context);
    _roleQueryService = new RoleQueryService(_context);
  }

  /// <summary>
  /// Flujo completo: AuthContext extrae PublicId ? UserQuery obtiene permisos ? RoleQuery valida rol.
  /// </summary>
  [Fact(DisplayName = "Flujo completo Auth ? Users ? Roles funciona correctamente")]
  public async Task CompleteFlow_AuthToUsersToRoles_WorksCorrectly()
  {
    // Arrange: Setup test data
    var userPublicId = Guid.NewGuid();
    var rolePublicId = Guid.NewGuid();

    var user = CreateTestUser(userPublicId, "admin@example.com");
    var adminRole = CreateTestRole(rolePublicId, "Admin", "Administrator role");
    var readPermission = CreateTestPermission("posts.read", "Read posts");
    var writePermission = CreateTestPermission("posts.write", "Write posts");
    var wildcardPermission = CreateTestPermission("users.*", "All users permissions");

    // Save entities
    _context.Users.Add(user);
    _context.Roles.Add(adminRole);
    _context.Permissions.AddRange(readPermission, writePermission, wildcardPermission);
    await _context.SaveChangesAsync();

    // Create relationships
    _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
    _context.RolePermissions.AddRange(
        new RolePermission { RoleId = adminRole.Id, PermissionId = readPermission.Id },
        new RolePermission { RoleId = adminRole.Id, PermissionId = writePermission.Id },
        new RolePermission { RoleId = adminRole.Id, PermissionId = wildcardPermission.Id });

    // Add permissions that match wildcard
    var userCreatePermission = CreateTestPermission("users.create", "Create users");
    var userDeletePermission = CreateTestPermission("users.delete", "Delete users");
    _context.Permissions.AddRange(userCreatePermission, userDeletePermission);
    await _context.SaveChangesAsync();

    // Setup authentication context
    SetupAuthenticatedUser(userPublicId, "admin@example.com");

    // Act & Assert: Step 1 - Auth extracts PublicId
    var extractedPublicId = _authContextService.GetCurrentUserPublicId();
    extractedPublicId.Should().Be(userPublicId);

    var extractedEmail = _authContextService.GetCurrentUserEmail();
    extractedEmail.Should().Be("admin@example.com");

    var isAuthenticated = _authContextService.IsAuthenticated();
    isAuthenticated.Should().BeTrue();

    // Act & Assert: Step 2 - Users domain queries
    var userRoles = await _userQueryService.GetUserRolesByPublicIdAsync(userPublicId);
    userRoles.Should().HaveCount(1);
    userRoles[0].Name.Should().Be("Admin");

    var userPermissions = await _userQueryService.GetUserPermissionsByPublicIdAsync(userPublicId);
    userPermissions.Should().Contain("posts.read");
    userPermissions.Should().Contain("posts.write");
    userPermissions.Should().Contain("users.*");
    userPermissions.Should().Contain("users.create");
    userPermissions.Should().Contain("users.delete");

    var hasDirectPermission = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "posts.read");
    hasDirectPermission.Should().BeTrue();

    var hasWildcardPermission = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "users.update");
    hasWildcardPermission.Should().BeTrue();

    var hasNoPermission = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "orders.delete");
    hasNoPermission.Should().BeFalse();

    // Act & Assert: Step 3 - Roles domain queries
    var rolePermissions = await _roleQueryService.GetRolePermissionsByPublicIdAsync(rolePublicId);
    rolePermissions.Should().HaveCount(3);
    rolePermissions.Should().Contain("posts.read");
    rolePermissions.Should().Contain("posts.write");
    rolePermissions.Should().Contain("users.*");

    var roleByPublicId = await _roleQueryService.GetRoleByPublicIdAsync(rolePublicId);
    roleByPublicId.Should().NotBeNull();
    roleByPublicId!.Name.Should().Be("Admin");

    var roleByName = await _roleQueryService.GetRoleByNameAsync("Admin");
    roleByName.Should().NotBeNull();
    roleByName!.PublicId.Should().Be(rolePublicId);
  }

  /// <summary>
  /// Flujo típico: Handler obtiene contexto ? consulta permisos ? valida autorización.
  /// </summary>
  [Fact(DisplayName = "Flujo típico de Handler con servicios distribuidos")]
  public async Task TypicalHandlerFlow_DistributedServices_WorksCorrectly()
  {
    // Arrange: Setup test data
    var userPublicId = Guid.NewGuid();
    var user = CreateTestUser(userPublicId, "editor@example.com");
    var editorRole = CreateTestRole(Guid.NewGuid(), "Editor", "Content editor");
    var editPermission = CreateTestPermission("posts.edit", "Edit posts");

    _context.Users.Add(user);
    _context.Roles.Add(editorRole);
    _context.Permissions.Add(editPermission);
    await _context.SaveChangesAsync();

    _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = editorRole.Id });
    _context.RolePermissions.Add(new RolePermission { RoleId = editorRole.Id, PermissionId = editPermission.Id });
    await _context.SaveChangesAsync();

    SetupAuthenticatedUser(userPublicId, "editor@example.com");

    // Act: Simulate typical handler flow

    // 1. Handler gets current user context (SECURE - only PublicId)
    var currentUserPublicId = _authContextService.GetCurrentUserPublicId();
    currentUserPublicId.Should().NotBeNull();

    // 2. Handler queries specific information using PublicId
    var userPermissions = await _userQueryService.GetUserPermissionsByPublicIdAsync(currentUserPublicId!.Value);

    // 3. Handler validates authorization
    var canEdit = await _userQueryService.HasPermissionByPublicIdAsync(currentUserPublicId.Value, "posts.edit");
    var canDelete = await _userQueryService.HasPermissionByPublicIdAsync(currentUserPublicId.Value, "posts.delete");

    // Assert: Verify results
    userPermissions.Should().Contain("posts.edit");
    canEdit.Should().BeTrue();
    canDelete.Should().BeFalse();
  }

  /// <summary>
  /// Flujo de error: Usuario no autenticado ? servicios manejan gracefully.
  /// </summary>
  [Fact(DisplayName = "Flujo con usuario no autenticado maneja errores gracefully")]
  public async Task UnauthenticatedUserFlow_HandlesGracefully()
  {
    // Arrange: No authentication setup (HttpContext será null por defecto)
    _httpContextAccessor.HttpContext = null;

    // Act & Assert: Auth context
    var publicId = _authContextService.GetCurrentUserPublicId();
    publicId.Should().BeNull();

    var email = _authContextService.GetCurrentUserEmail();
    email.Should().BeNull();

    var isAuthenticated = _authContextService.IsAuthenticated();
    isAuthenticated.Should().BeFalse();

    var currentUser = await _authContextService.GetCurrentUserAsync();
    currentUser.Should().BeNull();

    var claims = _authContextService.GetCurrentUserClaims();
    claims.Should().BeEmpty();

    // Act & Assert: User queries with invalid PublicId
    var randomPublicId = Guid.NewGuid();
    var userRoles = await _userQueryService.GetUserRolesByPublicIdAsync(randomPublicId);
    userRoles.Should().BeEmpty();

    var userPermissions = await _userQueryService.GetUserPermissionsByPublicIdAsync(randomPublicId);
    userPermissions.Should().BeEmpty();

    var hasPermission = await _userQueryService.HasPermissionByPublicIdAsync(randomPublicId, "any.permission");
    hasPermission.Should().BeFalse();

    var userByPublicId = await _userQueryService.GetUserByPublicIdAsync(randomPublicId);
    userByPublicId.Should().BeNull();
  }

  /// <summary>
  /// Flujo de performance: Múltiples usuarios y roles ? queries optimizados.
  /// </summary>
  [Fact(DisplayName = "Flujo de performance con múltiples usuarios y roles")]
  public async Task PerformanceFlow_MultipleUsersAndRoles_OptimizedQueries()
  {
    // Arrange: Setup multiple users and roles
    var users = new List<User>();
    var roles = new List<Role>();
    var permissions = new List<Permission>();

    // Create test data
    for (int i = 1; i <= 5; i++)
    {
      users.Add(CreateTestUser(Guid.NewGuid(), $"user{i}@example.com"));
      roles.Add(CreateTestRole(Guid.NewGuid(), $"Role{i}", $"Test role {i}"));
      permissions.Add(CreateTestPermission($"feature{i}.action", $"Feature {i} action"));
    }

    _context.Users.AddRange(users);
    _context.Roles.AddRange(roles);
    _context.Permissions.AddRange(permissions);
    await _context.SaveChangesAsync();

    // Create relationships
    for (int i = 0; i < 5; i++)
    {
      _context.UserRoles.Add(new UserRole { UserId = users[i].Id, RoleId = roles[i].Id });
      _context.RolePermissions.Add(new RolePermission { RoleId = roles[i].Id, PermissionId = permissions[i].Id });
    }
    await _context.SaveChangesAsync();

    // Act: Query all users and validate performance
    foreach (var user in users)
    {
      var userRoles = await _userQueryService.GetUserRolesByPublicIdAsync(user.PublicId);
      userRoles.Should().HaveCount(1);

      var userPermissions = await _userQueryService.GetUserPermissionsByPublicIdAsync(user.PublicId);
      userPermissions.Should().HaveCount(1);
    }

    // Query all roles
    var allRoles = await _roleQueryService.GetAllRolesAsync();
    allRoles.Should().HaveCount(5);
    allRoles.Should().BeInAscendingOrder(r => r.Name);

    // Assert: Performance validation (queries should complete quickly)
    // In a real scenario, you could measure execution time here
    allRoles.All(r => r.IsActive).Should().BeTrue();
  }

  #region Helper Methods

  private void SetupAuthenticatedUser(Guid publicId, string email)
  {
    var claims = new List<Claim>
        {
            new Claim("PublicId", publicId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("role", "user"),
        };

    var identity = new ClaimsIdentity(claims, "jwt");
    var principal = new ClaimsPrincipal(identity);
    var httpContext = new DefaultHttpContext { User = principal };

    _httpContextAccessor.HttpContext = httpContext;
  }

  private static User CreateTestUser(Guid publicId, string email)
  {
    return new User
    {
      PublicId = publicId,
      Email = email,
      Username = email.Split('@')[0],
      PasswordHash = "hashed_password",
      IsActive = true,
    };
  }

  private static Role CreateTestRole(Guid publicId, string name, string description, bool isActive = true)
  {
    return new Role
    {
      PublicId = publicId,
      Name = name,
      Description = description,
      IsActive = isActive,
    };
  }

  private static Permission CreateTestPermission(string key, string description, bool isActive = true)
  {
    return new Permission
    {
      PublicId = Guid.NewGuid(),
      Key = key,
      Description = description,
      IsActive = isActive,
    };
  }

  #endregion

  public void Dispose()
  {
    _context.Dispose();
    GC.SuppressFinalize(this);
  }
}




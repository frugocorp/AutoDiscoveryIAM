using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

public interface IPasswordManagementRepository
{
    Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    /// <summary>Busca por el hash del token de restablecimiento (en la base de datos no hay tokens en claro).</summary>
    Task<User?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
}

public sealed class PasswordManagementRepository(AutoDiscoveryIAMDbContext context) : IPasswordManagementRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken)
      => _context.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.PublicId == publicId, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
      => _context.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == email, cancellationToken);

    public Task<User?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
      => _context.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.PasswordResetToken == tokenHash, cancellationToken);
}

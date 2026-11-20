using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;

public interface IActivationRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    /// <summary>Busca por el hash del token de activación (en la base de datos no hay tokens en claro).</summary>
    Task<User?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
}

public sealed class ActivationRepository(AutoDiscoveryIAMDbContext context) : IActivationRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
      => _context.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == email, cancellationToken);

    public Task<User?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
      => _context.Users.FirstOrDefaultAsync(
        u => !u.IsDeleted && u.EmailVerificationToken == tokenHash,
        cancellationToken);

    public Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken)
      => _context.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.PublicId == publicId, cancellationToken);
}

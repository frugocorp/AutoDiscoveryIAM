using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;

public interface ITwoFactorRepository
{
  Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
}

public sealed class TwoFactorRepository(AutoDiscoveryIAMDbContext context) : ITwoFactorRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken)
    => _context.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.PublicId == publicId, cancellationToken);
}

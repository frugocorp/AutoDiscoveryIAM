// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;

public interface IDeleteMyAccountRepository
{
  Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
  void DeleteAccount(User user);
}

public class DeleteMyAccountRepository(AutoDiscoveryIAMDbContext context) : IDeleteMyAccountRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken) =>
    _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId && !u.IsDeleted, cancellationToken);

  public void DeleteAccount(User user)
  {
    // El AutoDiscoveryIAMDbContext convierte EntityState.Deleted en soft-delete automáticamente
    // (ApplyAuditInfoByState), igual que el resto de entidades principales.
    _context.Users.Remove(user);
  }
}

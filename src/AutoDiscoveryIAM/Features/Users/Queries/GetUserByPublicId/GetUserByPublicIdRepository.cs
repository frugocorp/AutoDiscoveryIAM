// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

public interface IGetUserByPublicIdRepository
{
  Task<User?> GetUserByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
}

public class GetUserByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IGetUserByPublicIdRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public async Task<User?> GetUserByPublicIdAsync(Guid publicId, CancellationToken cancellationToken)
  {
    return await _context.Users
        .FirstOrDefaultAsync(u => u.PublicId == publicId && !u.IsDeleted, cancellationToken);
  }
}

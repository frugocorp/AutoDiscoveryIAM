// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

public interface IUpdateMyProfileRepository
{
  Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
}

public class UpdateMyProfileRepository(AutoDiscoveryIAMDbContext context) : IUpdateMyProfileRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken) =>
    _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId && !u.IsDeleted, cancellationToken);
}

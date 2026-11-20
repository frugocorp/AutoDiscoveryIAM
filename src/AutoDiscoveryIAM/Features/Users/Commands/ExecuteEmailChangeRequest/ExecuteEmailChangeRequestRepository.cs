// -----------------------------------------------------------------------
// <copyright file="ExecuteEmailChangeRequestRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;

public interface IExecuteEmailChangeRequestRepository
{
  Task<EmailChangeRequest?> GetByPublicIdWithUserAsync(Guid publicId, CancellationToken cancellationToken);
  Task<bool> ExistsEmailAsync(string email, long excludingUserId, CancellationToken cancellationToken);
}

public class ExecuteEmailChangeRequestRepository(AutoDiscoveryIAMDbContext context) : IExecuteEmailChangeRequestRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<EmailChangeRequest?> GetByPublicIdWithUserAsync(Guid publicId, CancellationToken cancellationToken) =>
    _context.EmailChangeRequests
      .Include(r => r.User)
      .FirstOrDefaultAsync(r => r.PublicId == publicId, cancellationToken);

  public Task<bool> ExistsEmailAsync(string email, long excludingUserId, CancellationToken cancellationToken) =>
    _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && u.Id != excludingUserId, cancellationToken);
}

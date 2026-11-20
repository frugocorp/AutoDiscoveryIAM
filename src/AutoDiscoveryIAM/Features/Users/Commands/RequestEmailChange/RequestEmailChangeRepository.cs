// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;

public interface IRequestEmailChangeRepository
{
  Task<User?> GetUserByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
  Task<bool> ExistsEmailAsync(string email, long excludingUserId, CancellationToken cancellationToken);
  Task<bool> HasPendingRequestAsync(long userId, CancellationToken cancellationToken);
  Task AddAsync(EmailChangeRequest request, CancellationToken cancellationToken);
}

public class RequestEmailChangeRepository(AutoDiscoveryIAMDbContext context) : IRequestEmailChangeRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<User?> GetUserByPublicIdAsync(Guid publicId, CancellationToken cancellationToken) =>
    _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

  public Task<bool> ExistsEmailAsync(string email, long excludingUserId, CancellationToken cancellationToken) =>
    _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && u.Id != excludingUserId, cancellationToken);

  public Task<bool> HasPendingRequestAsync(long userId, CancellationToken cancellationToken) =>
    _context.EmailChangeRequests.AnyAsync(
      r => r.UserId == userId
        && (r.Status == EmailChangeRequestStatusCatalog.Pending || r.Status == EmailChangeRequestStatusCatalog.ApprovedForExecution),
      cancellationToken);

  public async Task AddAsync(EmailChangeRequest request, CancellationToken cancellationToken)
  {
    await _context.EmailChangeRequests.AddAsync(request, cancellationToken);
  }
}

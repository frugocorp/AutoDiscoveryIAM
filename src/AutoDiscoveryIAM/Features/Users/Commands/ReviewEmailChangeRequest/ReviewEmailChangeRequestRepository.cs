// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

public interface IReviewEmailChangeRequestRepository
{
  Task<EmailChangeRequest?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
}

public class ReviewEmailChangeRequestRepository(AutoDiscoveryIAMDbContext context) : IReviewEmailChangeRequestRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<EmailChangeRequest?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken) =>
    _context.EmailChangeRequests.FirstOrDefaultAsync(r => r.PublicId == publicId, cancellationToken);
}

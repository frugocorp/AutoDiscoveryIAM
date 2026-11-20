using Microsoft.EntityFrameworkCore;
using TuProyecto.Data;
using TuProyecto.Features.UserProfiles.Models.Entities;

namespace TuProyecto.Features.UserProfiles.Services.UserProfileQuery;

public class UserProfileQueryService(ApplicationDbContext context) : IUserProfileQueryService
{
  private readonly ApplicationDbContext _context = context;

  public async Task<UserProfile?> GetUserProfileByPublicIdAsync(Guid userProfilePublicId)
  {
    return await _context.UserProfiles.FirstOrDefaultAsync(p => p.PublicId == userProfilePublicId);
  }

  public async Task<bool> UserProfileExistsAsync(Guid userProfilePublicId)
  {
    return await _context.UserProfiles.AnyAsync(p => p.PublicId == userProfilePublicId);
  }
}

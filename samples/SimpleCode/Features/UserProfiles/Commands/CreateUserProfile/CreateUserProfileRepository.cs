using Microsoft.EntityFrameworkCore;
using TuProyecto.Data;
using TuProyecto.Features.UserProfiles.Models.Entities;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public interface ICreateUserProfileRepository
{
  Task<long?> GetUserIdByPublicIdAsync(Guid userPublicId);
  Task<UserProfile> CreateUserProfile(UserProfile userProfile);
}

public class CreateUserProfileRepository(ApplicationDbContext context) : ICreateUserProfileRepository
{
  private readonly ApplicationDbContext _context = context;

  public async Task<long?> GetUserIdByPublicIdAsync(Guid userPublicId)
  {
    // _context.Users viene de AutoDiscoveryIAMDbContext -- disponible aquí sin un segundo DbContext
    // ni una segunda consulta, precisamente porque ApplicationDbContext lo hereda (contexto único).
    return await _context.Users
      .Where(u => u.PublicId == userPublicId)
      .Select(u => (long?)u.Id)
      .FirstOrDefaultAsync();
  }

  public Task<UserProfile> CreateUserProfile(UserProfile userProfile)
  {
    _context.UserProfiles.Add(userProfile);
    return Task.FromResult(userProfile);
  }
}

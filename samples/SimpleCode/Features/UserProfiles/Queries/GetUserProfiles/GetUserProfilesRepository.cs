using Microsoft.EntityFrameworkCore;
using TuProyecto.Data;
using TuProyecto.Features.UserProfiles.Models.Entities;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public interface IGetUserProfilesRepository
{
  Task<(List<UserProfile> UserProfiles, int TotalCount)> GetUserProfiles(int pageNumber, int pageSize);
}

public class GetUserProfilesRepository(ApplicationDbContext context) : IGetUserProfilesRepository
{
  private readonly ApplicationDbContext _context = context;

  public async Task<(List<UserProfile> UserProfiles, int TotalCount)> GetUserProfiles(int pageNumber, int pageSize)
  {
    // .Include(p => p.User) es un join nativo de EF Core -- solo funciona porque UserProfile.User y
    // AutoDiscoveryIAMDbContext.Users viven en el MISMO modelo (contexto único). Con dos DbContexts
    // separados esto no compila: tocaría un join en memoria o SQL directo (ver "Elige tu propio
    // DbContext: uno o dos" en el README de AutoDiscoveryIAM).
    var query = _context.UserProfiles.Include(p => p.User).OrderByDescending(p => p.CreatedAt);
    var totalCount = await query.CountAsync();
    var userProfiles = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
    return (userProfiles, totalCount);
  }
}

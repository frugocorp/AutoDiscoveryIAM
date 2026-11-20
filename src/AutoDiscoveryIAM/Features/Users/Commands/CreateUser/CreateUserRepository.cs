// -----------------------------------------------------------------------
// <copyright file="CreateUserRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;

public interface ICreateUserRepository
{
  public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
  public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);
  Task<User> CreateUser(User user);
}

public class CreateUserRepository(AutoDiscoveryIAMDbContext context) : ICreateUserRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
  {
    // IgnoreQueryFilters(): el índice único no distingue cuentas borradas suavemente.
    return await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken);
  }

  public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
  {
    return await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username, cancellationToken);
  }
  public Task<User> CreateUser(User user)
  {
    _context.Users.Add(user);
    return Task.FromResult(user);
  }
}

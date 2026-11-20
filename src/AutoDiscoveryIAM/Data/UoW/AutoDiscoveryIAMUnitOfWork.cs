using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW
{
  /// <summary>
  /// Implementación de Unit of Work para control transaccional explícito.
  /// </summary>
  public class AutoDiscoveryIAMUnitOfWork(AutoDiscoveryIAMDbContext context) : IAutoDiscoveryIAMUnitOfWork
  {
    private readonly AutoDiscoveryIAMDbContext _context = context;
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
      _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
      if (_transaction != null)
        await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
      if (_transaction != null)
        await _transaction.RollbackAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task<int> SaveAndCommitChangesAsync(CancellationToken cancellationToken = default)
    {
      var result = await SaveChangesAsync(cancellationToken);
      await CommitAsync(cancellationToken);
      return result;
    }

    public void Dispose()
    {
      _transaction?.Dispose();
      _context.Dispose();
      GC.SuppressFinalize(this);
    }
  }
}

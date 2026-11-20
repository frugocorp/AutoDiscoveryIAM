using System;
using System.Threading;
using System.Threading.Tasks;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW
{
  /// <summary>
  /// Contrato para Unit of Work, permite control transaccional explícito.
  /// </summary>
  public interface IAutoDiscoveryIAMUnitOfWork : IDisposable
  {
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> SaveAndCommitChangesAsync(CancellationToken cancellationToken = default);
  }
}

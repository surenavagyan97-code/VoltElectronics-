namespace VoltElectronics.Domain.Common;

/// <summary>Commits every aggregate change made during one request as a single transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

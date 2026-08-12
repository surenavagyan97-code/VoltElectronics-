using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Infrastructure.Data;

/// <summary>
/// One DbContext save per request-scoped unit of work. Domain events are dispatched *before* the
/// save, so whatever their handlers change commits atomically with the state change that raised
/// them — an order can't be marked paid without its stock draw-down, or vice versa.
/// </summary>
internal sealed class EfUnitOfWork(AppDbContext db, IDomainEventDispatcher events) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handlers may themselves raise further events on other aggregates; loop until quiet.
        while (true)
        {
            var raised = db.ChangeTracker.Entries<AggregateRoot>()
                .Select(e => e.Entity)
                .Where(a => a.DomainEvents.Count > 0)
                .ToList();
            if (raised.Count == 0) break;

            var batch = raised.SelectMany(a => a.DomainEvents).ToList();
            raised.ForEach(a => a.ClearDomainEvents());
            await events.DispatchAsync(batch, cancellationToken);
        }

        return await db.SaveChangesAsync(cancellationToken);
    }
}

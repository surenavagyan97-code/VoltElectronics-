using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Common.Messaging;

/// <summary>
/// Reacts to something an aggregate recorded. Handlers run inside the same transaction as the
/// change that raised the event, so a handler's own writes commit or roll back with it.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}

/// <summary>
/// Fans each event out to every handler registered for its concrete type. Unlike commands, an event
/// may have any number of handlers — or none, which is not an error.
/// </summary>
internal sealed class DomainEventDispatcher(IServiceProvider provider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Fanout> Fanouts = new();

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var fanout = Fanouts.GetOrAdd(domainEvent.GetType(), static type =>
                (Fanout)Activator.CreateInstance(typeof(Fanout<>).MakeGenericType(type))!);
            await fanout.Invoke(provider, domainEvent, cancellationToken);
        }
    }

    private abstract class Fanout
    {
        public abstract Task Invoke(IServiceProvider provider, IDomainEvent domainEvent, CancellationToken ct);
    }

    private sealed class Fanout<TEvent> : Fanout
        where TEvent : IDomainEvent
    {
        public override async Task Invoke(IServiceProvider provider, IDomainEvent domainEvent, CancellationToken ct)
        {
            foreach (var handler in provider.GetServices<IDomainEventHandler<TEvent>>())
                await handler.HandleAsync((TEvent)domainEvent, ct);
        }
    }
}

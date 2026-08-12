using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace VoltElectronics.Application.Common.Messaging;

/// <summary>
/// Resolves handlers by the message's runtime type. Callers only ever see <c>ICommand&lt;TResult&gt;</c>,
/// so the concrete command type — which is what the handler is registered against — has to be
/// recovered at runtime. A closed generic adapter per message type does that once and is cached,
/// keeping the per-dispatch cost to a dictionary lookup and one interface call.
/// </summary>
internal sealed class Dispatcher(IServiceProvider provider) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> CommandAdapters = new();
    private static readonly ConcurrentDictionary<Type, object> QueryAdapters = new();

    public Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var adapter = (Adapter<TResult>)CommandAdapters.GetOrAdd(
            command.GetType(),
            static type => Create<TResult>(typeof(CommandAdapter<,>), type));
        return adapter.Invoke(provider, command, cancellationToken);
    }

    public Task<TResult> Query<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var adapter = (Adapter<TResult>)QueryAdapters.GetOrAdd(
            query.GetType(),
            static type => Create<TResult>(typeof(QueryAdapter<,>), type));
        return adapter.Invoke(provider, query, cancellationToken);
    }

    private static object Create<TResult>(Type openAdapter, Type messageType) =>
        Activator.CreateInstance(openAdapter.MakeGenericType(messageType, typeof(TResult)))!;

    private abstract class Adapter<TResult>
    {
        public abstract Task<TResult> Invoke(IServiceProvider provider, object message, CancellationToken ct);
    }

    private sealed class CommandAdapter<TCommand, TResult> : Adapter<TResult>
        where TCommand : ICommand<TResult>
    {
        public override Task<TResult> Invoke(IServiceProvider provider, object message, CancellationToken ct) =>
            provider.GetRequiredService<ICommandHandler<TCommand, TResult>>()
                .HandleAsync((TCommand)message, ct);
    }

    private sealed class QueryAdapter<TQuery, TResult> : Adapter<TResult>
        where TQuery : IQuery<TResult>
    {
        public override Task<TResult> Invoke(IServiceProvider provider, object message, CancellationToken ct) =>
            provider.GetRequiredService<IQueryHandler<TQuery, TResult>>()
                .HandleAsync((TQuery)message, ct);
    }
}

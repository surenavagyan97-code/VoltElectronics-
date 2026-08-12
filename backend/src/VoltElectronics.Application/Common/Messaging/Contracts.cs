namespace VoltElectronics.Application.Common.Messaging;

/// <summary>
/// A request that changes state. One command, one handler, one transaction.
/// </summary>
public interface ICommand<out TResult>;

/// <summary>
/// A read-only request. Query handlers live in the persistence layer because a projection is a
/// storage concern — the shape a screen needs rarely matches an aggregate's shape.
/// </summary>
public interface IQuery<out TResult>;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

/// <summary>Routes a command or query to its single registered handler.</summary>
public interface IDispatcher
{
    Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    Task<TResult> Query<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}

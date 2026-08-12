using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Infrastructure.Data;

/// <summary>
/// Transaction middleware for the write side: every command runs inside one database transaction,
/// begun here and committed once the handler finishes — including handlers that save more than
/// once (checkout persists the order, calls the gateway, then records the payment id; those now
/// stand or fall together). An unhandled exception rolls the whole command back on dispose.
///
/// Wired as a Scrutor decorator over <see cref="ICommandHandler{TCommand,TResult}"/>, so every
/// current and future command is covered without opting in. Queries are read-only and stay out.
/// </summary>
internal sealed class TransactionBehavior<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    AppDbContext db) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        // A command sent from inside another command joins the transaction already in flight.
        if (db.Database.CurrentTransaction is not null)
            return await inner.HandleAsync(command, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await inner.HandleAsync(command, cancellationToken);

        // Business failures (a Result carrying an error) still commit: their handlers may have
        // recorded state worth keeping, like an order abandoned because the gateway was down.
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}

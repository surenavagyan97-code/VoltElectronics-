using FluentValidation;
using VoltElectronics.Application.Common.Results;

namespace VoltElectronics.Application.Common.Messaging;

/// <summary>
/// Runs every registered FluentValidation validator for a command before its handler. Failures
/// come back as the command's own failed <see cref="Result"/> — the same 400 <c>{ error }</c>
/// shape handlers produce — so the storefront can't tell who rejected the request.
///
/// Wired as the outermost <see cref="ICommandHandler{TCommand,TResult}"/> decorator (outside
/// <c>TransactionBehavior</c>): an invalid command is turned away before a transaction is opened.
/// Commands without validators pass straight through.
/// </summary>
public sealed class ValidationBehavior<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        foreach (var validator in validators)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
                return Failure(Error.Invalid(string.Join(" ",
                    validation.Errors.Select(e => e.ErrorMessage).Distinct())));
        }

        return await inner.HandleAsync(command, cancellationToken);
    }

    // Commands return Result or Result<T>; resolved once per closed generic type.
    private static readonly Func<Error, TResult>? FailureFactory = CreateFailureFactory();

    private static TResult Failure(Error error) =>
        FailureFactory is not null
            ? FailureFactory(error)
            : throw new InvalidOperationException(
                $"{typeof(TCommand).Name} has a validator, but its result type " +
                $"{typeof(TResult).Name} cannot carry a validation failure.");

    private static Func<Error, TResult>? CreateFailureFactory()
    {
        if (typeof(TResult) == typeof(Result))
            return error => (TResult)(object)Result.Failure(error);

        if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failure = typeof(TResult).GetMethod(nameof(Result.Failure), [typeof(Error)])!;
            return error => (TResult)failure.Invoke(null, [error])!;
        }

        return null;
    }
}

using VoltElectronics.Application.Common.Abstractions;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Carts.Commands;

/// <summary>The cart's currency is what checkout charges in, so it's stored, not a display setting.</summary>
public sealed record SetCartCurrencyCommand(CartKey Key, string Currency) : ICommand<Result<CartDto>>;

internal sealed class SetCartCurrencyHandler(
    ICartRepository carts,
    ICurrencyConverter currency,
    ICartReader reader,
    IUnitOfWork unitOfWork) : ICommandHandler<SetCartCurrencyCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> HandleAsync(SetCartCurrencyCommand command, CancellationToken cancellationToken)
    {
        if (!currency.IsSupported(command.Currency))
            return Error.Invalid($"Unsupported currency \"{command.Currency}\".");

        var cart = await carts.FindAsync(command.Key, cancellationToken) ?? carts.Start(command.Key);
        cart.ChangeCurrency(command.Currency);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await reader.ReadAsync(command.Key, cancellationToken);
    }
}

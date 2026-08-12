using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Carts.Commands;

public sealed record RemoveCartItemCommand(CartKey Key, int ProductId) : ICommand<Result<CartDto>>;

/// <summary>Idempotent — removing a line that isn't there just returns the cart.</summary>
internal sealed class RemoveCartItemHandler(
    ICartRepository carts,
    ICartReader reader,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveCartItemCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> HandleAsync(RemoveCartItemCommand command, CancellationToken cancellationToken)
    {
        var cart = await carts.FindAsync(command.Key, cancellationToken);
        if (cart is not null)
        {
            cart.RemoveItem(command.ProductId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await reader.ReadAsync(command.Key, cancellationToken);
    }
}

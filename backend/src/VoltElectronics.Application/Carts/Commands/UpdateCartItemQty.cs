using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Carts.Commands;

/// <summary>A quantity below 1 drops the line, matching the storefront's stepper.</summary>
public sealed record UpdateCartItemQtyCommand(CartKey Key, int ProductId, int Qty) : ICommand<Result<CartDto>>;

internal sealed class UpdateCartItemQtyHandler(
    ICartRepository carts,
    IProductRepository products,
    ICartReader reader,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCartItemQtyCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> HandleAsync(UpdateCartItemQtyCommand command, CancellationToken cancellationToken)
    {
        var cart = await carts.FindAsync(command.Key, cancellationToken);
        if (cart is null) return Error.Invalid("Cart not found.");

        // A product pulled from the catalog since the line was added leaves no stock to raise to,
        // but the shopper must still be able to remove the line — hence 0 rather than a hard failure.
        var product = await products.GetByIdAsync(command.ProductId, cancellationToken);
        cart.SetItemQty(command.ProductId, command.Qty, product?.Stock ?? 0);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await reader.ReadAsync(command.Key, cancellationToken);
    }
}

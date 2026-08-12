using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Carts.Commands;

public sealed record AddCartItemCommand(CartKey Key, int ProductId, int Qty) : ICommand<Result<CartDto>>;

internal sealed class AddCartItemHandler(
    ICartRepository carts,
    IProductRepository products,
    ICartReader reader,
    IUnitOfWork unitOfWork) : ICommandHandler<AddCartItemCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> HandleAsync(AddCartItemCommand command, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null || !product.IsPurchasable) return Error.Invalid("Product not found.");

        var cart = await carts.FindAsync(command.Key, cancellationToken) ?? carts.Start(command.Key);

        // Quantity and stock rules are the aggregate's; a DomainException from here surfaces as a 400.
        cart.AddItem(product.Id, command.Qty, product.Stock);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await reader.ReadAsync(command.Key, cancellationToken);
    }
}

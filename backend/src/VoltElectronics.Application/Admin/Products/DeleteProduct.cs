using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Admin.Products;

/// <summary>Archives the product when it has order history; hard-deletes it otherwise.</summary>
public sealed record DeleteProductCommand(int Id) : ICommand<Result>;

internal sealed class DeleteProductHandler(
    IProductRepository products,
    ICartRepository carts,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteProductCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null) return Error.Invalid("Product not found.");

        if (await products.HasOrderHistoryAsync(command.Id, cancellationToken))
        {
            // Placed orders quote the product by id; archiving retires it without orphaning them.
            product.Archive();
        }
        else
        {
            await carts.RemoveProductLinesAsync(command.Id, cancellationToken);
            products.Remove(product);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

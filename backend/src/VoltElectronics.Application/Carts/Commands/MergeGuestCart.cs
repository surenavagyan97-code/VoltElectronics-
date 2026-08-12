using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Carts.Commands;

/// <summary>Folds the pre-login guest cart into the authenticated user's cart.</summary>
public sealed record MergeGuestCartCommand(Guid GuestCartId, string UserId) : ICommand<Result<CartDto>>;

internal sealed class MergeGuestCartHandler(
    ICartRepository carts,
    IProductRepository products,
    ICartReader reader,
    IUnitOfWork unitOfWork) : ICommandHandler<MergeGuestCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> HandleAsync(MergeGuestCartCommand command, CancellationToken cancellationToken)
    {
        var userKey = new CartKey(command.UserId, null);

        var guestCart = await carts.GetGuestCartAsync(command.GuestCartId, cancellationToken);
        if (guestCart is null) return await reader.ReadAsync(userKey, cancellationToken);

        var userCart = await carts.GetByUserIdAsync(command.UserId, cancellationToken);
        if (userCart is null)
        {
            // Cheapest merge path: an unclaimed guest cart simply becomes the user's cart.
            guestCart.AssignTo(command.UserId);
        }
        else
        {
            // Combined quantities are clamped to what's actually in stock, so the merge needs
            // current stock for every product arriving from the guest cart.
            var incoming = guestCart.Items.Select(i => i.ProductId).ToArray();
            var stockByProduct = (await products.GetByIdsAsync(incoming, cancellationToken))
                .ToDictionary(p => p.Id, p => p.Stock);

            userCart.AbsorbFrom(guestCart, stockByProduct);
            carts.Remove(guestCart);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await reader.ReadAsync(userKey, cancellationToken);
    }
}

using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Promotions;

namespace VoltElectronics.Application.Carts.Commands;

public sealed record ApplyCouponCommand(CartKey Key, string Code) : ICommand<Result<CartDto>>;

internal sealed class ApplyCouponHandler(
    ICartRepository carts,
    IProductRepository products,
    IPromotionRepository promotions,
    ICartReader reader,
    IUnitOfWork unitOfWork) : ICommandHandler<ApplyCouponCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> HandleAsync(ApplyCouponCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code)) return Error.Invalid("Enter a coupon code.");

        var promotion = await promotions.GetByCodeAsync(Promotion.Normalize(command.Code), cancellationToken);
        if (promotion is null) return Error.Invalid("Coupon code not found.");

        var cart = await carts.FindAsync(command.Key, cancellationToken) ?? carts.Start(command.Key);
        if (cart.Items.Count == 0) return Error.Invalid("Your cart is empty.");

        var catalog = await products.GetByIdsAsync(cart.Items.Select(i => i.ProductId).ToArray(), cancellationToken);
        var subtotalBase = cart.Items.Sum(i => catalog.First(p => p.Id == i.ProductId).Price * i.Qty);

        var error = promotion.Scope == PromotionScope.Order
            ? promotion.ValidateForOrder(subtotalBase)
            : promotion.ValidateWindow();
        if (error is not null) return Error.Invalid(error);

        cart.ApplyCoupon(promotion.Code!);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await reader.ReadAsync(command.Key, cancellationToken);
    }
}

public sealed record RemoveCouponCommand(CartKey Key) : ICommand<Result<CartDto>>;

internal sealed class RemoveCouponHandler(
    ICartRepository carts, ICartReader reader, IUnitOfWork unitOfWork) : ICommandHandler<RemoveCouponCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> HandleAsync(RemoveCouponCommand command, CancellationToken cancellationToken)
    {
        var cart = await carts.FindAsync(command.Key, cancellationToken);
        if (cart is not null)
        {
            cart.RemoveCoupon();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return await reader.ReadAsync(command.Key, cancellationToken);
    }
}

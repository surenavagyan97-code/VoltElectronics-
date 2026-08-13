using FluentValidation;

namespace VoltElectronics.Application.Carts.Commands;

// Shape checks only — stock limits and product availability are the Cart aggregate's own rules.

internal sealed class AddCartItemValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemValidator()
    {
        RuleFor(c => c.ProductId).GreaterThan(0);
        RuleFor(c => c.Qty).GreaterThan(0);
    }
}

/// <summary>Qty 0 is legal here — it removes the line.</summary>
internal sealed class UpdateCartItemQtyValidator : AbstractValidator<UpdateCartItemQtyCommand>
{
    public UpdateCartItemQtyValidator()
    {
        RuleFor(c => c.ProductId).GreaterThan(0);
        RuleFor(c => c.Qty).GreaterThanOrEqualTo(0);
    }
}

internal sealed class SetCartCurrencyValidator : AbstractValidator<SetCartCurrencyCommand>
{
    public SetCartCurrencyValidator() =>
        RuleFor(c => c.Currency).NotEmpty().Length(3);
}

internal sealed class MergeGuestCartValidator : AbstractValidator<MergeGuestCartCommand>
{
    public MergeGuestCartValidator()
    {
        RuleFor(c => c.GuestCartId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
    }
}

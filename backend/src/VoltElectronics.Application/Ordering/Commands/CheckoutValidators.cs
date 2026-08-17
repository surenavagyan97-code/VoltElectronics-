using FluentValidation;

namespace VoltElectronics.Application.Ordering.Commands;

/// <summary>
/// The shipping form's shape rules; lengths mirror the ShippingAddress column limits. Cart
/// contents and stock are checked in the handler where the database is at hand.
/// </summary>
internal sealed class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(r => r.FullName).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Company).MaximumLength(150);
        RuleFor(r => r.Street).NotEmpty().MaximumLength(250);
        RuleFor(r => r.City).NotEmpty().MaximumLength(100);
        RuleFor(r => r.State).NotEmpty().MaximumLength(50);
        RuleFor(r => r.Zip).NotEmpty().MaximumLength(20);
        RuleFor(r => r.Phone).NotEmpty().MaximumLength(30);
    }
}

internal sealed class CheckoutValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutValidator() =>
        RuleFor(c => c.ShipTo).SetValidator(new CheckoutRequestValidator());
}

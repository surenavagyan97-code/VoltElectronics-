using FluentValidation;
using VoltElectronics.Application.Promotions;

namespace VoltElectronics.Application.Admin.Promotions;

internal sealed class SavePromotionRequestValidator : AbstractValidator<SavePromotionRequest>
{
    public SavePromotionRequestValidator()
    {
        RuleFor(r => r.Code).MaximumLength(30).Matches("^[A-Za-z0-9_-]+$")
            .When(r => !string.IsNullOrWhiteSpace(r.Code))
            .WithMessage("Code can only contain letters, numbers, hyphens and underscores.");
        RuleFor(r => r.Type).Must(t => t is "Percentage" or "FixedAmount").WithMessage("Type must be Percentage or FixedAmount.");
        RuleFor(r => r.Scope).Must(s => s is "Order" or "Category" or "Product").WithMessage("Scope must be Order, Category or Product.");
        RuleFor(r => r.Value).GreaterThan(0);
        RuleFor(r => r.Value).LessThanOrEqualTo(100).When(r => r.Type == "Percentage").WithMessage("A percentage discount can't exceed 100.");
        RuleFor(r => r.CategoryId).NotNull().When(r => r.Scope == "Category").WithMessage("Choose a category.");
        RuleFor(r => r.ProductIds).Must(ids => ids.Count > 0).When(r => r.Scope == "Product").WithMessage("Choose at least one product.");
        RuleFor(r => r.MaxRedemptions).GreaterThan(0).When(r => r.MaxRedemptions is not null);
        RuleFor(r => r.MinSubtotal).GreaterThan(0).When(r => r.MinSubtotal is not null);
        RuleFor(r => r.MaxDiscountAmount).GreaterThan(0).When(r => r.MaxDiscountAmount is not null);
        RuleFor(r => r.ExpiresAt).GreaterThan(r => r.StartsAt!.Value)
            .When(r => r.StartsAt is not null && r.ExpiresAt is not null)
            .WithMessage("The end date must be after the start date.");
    }
}

internal sealed class CreatePromotionValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionValidator() => RuleFor(c => c.Promotion).SetValidator(new SavePromotionRequestValidator());
}

internal sealed class UpdatePromotionValidator : AbstractValidator<UpdatePromotionCommand>
{
    public UpdatePromotionValidator() => RuleFor(c => c.Promotion).SetValidator(new SavePromotionRequestValidator());
}

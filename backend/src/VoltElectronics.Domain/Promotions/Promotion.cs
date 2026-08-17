using VoltElectronics.Domain.Common;

namespace VoltElectronics.Domain.Promotions;

public enum PromotionType { Percentage, FixedAmount }

/// <summary>What a promotion discounts: the whole order's subtotal, everything in one category,
/// or a hand-picked set of products.</summary>
public enum PromotionScope { Order, Category, Product }

/// <summary>
/// A single discount mechanism that covers both "coupon codes" and admin-run "sales":
/// <see cref="Code"/> null means it applies automatically to every qualifying shopper with no
/// code needed (a sale); a code means the shopper must type it at checkout.
/// </summary>
public sealed class Promotion : AggregateRoot
{
    private readonly List<PromotionProduct> _products = [];

    private Promotion() { }

    public int Id { get; private set; }
    public string? Code { get; private set; }
    public string? Name { get; private set; }
    public PromotionType Type { get; private set; }
    public decimal Value { get; private set; }
    public PromotionScope Scope { get; private set; }
    public int? CategoryId { get; private set; }
    public decimal? MinSubtotal { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public int? MaxRedemptions { get; private set; }
    public int RedemptionCount { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<PromotionProduct> Products => _products;

    public bool RequiresCode => Code is not null;

    public static Promotion Create(
        string? code, string? name, PromotionType type, decimal value, PromotionScope scope,
        int? categoryId, IEnumerable<int> productIds, decimal? minSubtotal, decimal? maxDiscountAmount,
        int? maxRedemptions, DateTime? startsAt, DateTime? expiresAt)
    {
        var promotion = new Promotion { IsActive = true };
        promotion.Apply(code, name, type, value, scope, categoryId, productIds,
            minSubtotal, maxDiscountAmount, maxRedemptions, startsAt, expiresAt, true);
        return promotion;
    }

    public void Update(
        string? code, string? name, PromotionType type, decimal value, PromotionScope scope,
        int? categoryId, IEnumerable<int> productIds, decimal? minSubtotal, decimal? maxDiscountAmount,
        int? maxRedemptions, DateTime? startsAt, DateTime? expiresAt, bool isActive) =>
        Apply(code, name, type, value, scope, categoryId, productIds,
            minSubtotal, maxDiscountAmount, maxRedemptions, startsAt, expiresAt, isActive);

    private void Apply(
        string? code, string? name, PromotionType type, decimal value, PromotionScope scope,
        int? categoryId, IEnumerable<int> productIds, decimal? minSubtotal, decimal? maxDiscountAmount,
        int? maxRedemptions, DateTime? startsAt, DateTime? expiresAt, bool isActive)
    {
        if (value <= 0) throw new DomainException("Discount value must be greater than zero.");
        if (type == PromotionType.Percentage && value > 100) throw new DomainException("A percentage discount can't exceed 100.");
        if (scope == PromotionScope.Category && categoryId is null) throw new DomainException("Choose a category for a category-scoped promotion.");

        var ids = productIds.Distinct().ToList();
        if (scope == PromotionScope.Product && ids.Count == 0) throw new DomainException("Choose at least one product for a product-scoped promotion.");
        if (maxRedemptions is <= 0) throw new DomainException("Max redemptions must be at least 1 if set.");
        if (startsAt is not null && expiresAt is not null && expiresAt <= startsAt) throw new DomainException("The end date must be after the start date.");

        Code = string.IsNullOrWhiteSpace(code) ? null : Normalize(code);
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Type = type;
        Value = value;
        Scope = scope;
        CategoryId = scope == PromotionScope.Category ? categoryId : null;

        _products.Clear();
        if (scope == PromotionScope.Product)
            foreach (var id in ids) _products.Add(new PromotionProduct(id));

        MinSubtotal = scope == PromotionScope.Order && minSubtotal is > 0 ? minSubtotal : null;
        MaxDiscountAmount = type == PromotionType.Percentage && maxDiscountAmount is > 0 ? maxDiscountAmount : null;
        MaxRedemptions = maxRedemptions is > 0 ? maxRedemptions : null;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        IsActive = isActive;
    }

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    /// <summary>Active/scheduled/usage-limit checks that don't depend on the shopper's cart.</summary>
    public string? ValidateWindow()
    {
        if (!IsActive) return "This promotion is not active.";
        var now = DateTime.UtcNow;
        if (StartsAt is not null && now < StartsAt) return "This promotion hasn't started yet.";
        if (ExpiresAt is not null && now > ExpiresAt) return "This promotion has expired.";
        if (MaxRedemptions is not null && RedemptionCount >= MaxRedemptions) return "This promotion has reached its usage limit.";
        return null;
    }

    /// <summary>Full validation for an order-scoped promotion against the cart's base-currency subtotal.</summary>
    public string? ValidateForOrder(decimal subtotalBase)
    {
        var windowError = ValidateWindow();
        if (windowError is not null) return windowError;
        if (MinSubtotal is not null && subtotalBase < MinSubtotal) return "Your order doesn't meet this promotion's minimum amount.";
        return null;
    }

    /// <summary>Discount for an amount priced in the base currency — a line's unit price, or a subtotal.</summary>
    public decimal ComputeDiscount(decimal amountBase)
    {
        var discount = Type == PromotionType.Percentage ? Math.Round(amountBase * Value / 100m, 2) : Value;
        if (MaxDiscountAmount is not null) discount = Math.Min(discount, MaxDiscountAmount.Value);
        return Math.Clamp(discount, 0, amountBase);
    }

    public void Redeem()
    {
        if (MaxRedemptions is not null && RedemptionCount >= MaxRedemptions)
            throw new DomainException("This promotion has reached its usage limit.");
        RedemptionCount++;
    }
}

/// <summary>One product a product-scoped promotion targets.</summary>
public sealed class PromotionProduct
{
    private PromotionProduct() { }
    public PromotionProduct(int productId) => ProductId = productId;

    public int Id { get; private set; }
    public int PromotionId { get; private set; }
    public int ProductId { get; private set; }
}

namespace VoltElectronics.Application.Promotions;

public record PromotionDto(
    int Id, string? Code, string? Name, string Type, decimal Value, string Scope,
    int? CategoryId, string? CategoryName, IReadOnlyList<int> ProductIds,
    decimal? MinSubtotal, decimal? MaxDiscountAmount,
    int? MaxRedemptions, int RedemptionCount,
    DateTime? StartsAt, DateTime? ExpiresAt, bool IsActive, DateTime CreatedAt);

public record SavePromotionRequest(
    string? Code, string? Name, string Type, decimal Value, string Scope,
    int? CategoryId, IReadOnlyList<int> ProductIds,
    decimal? MinSubtotal, decimal? MaxDiscountAmount,
    int? MaxRedemptions, DateTime? StartsAt, DateTime? ExpiresAt, bool IsActive);

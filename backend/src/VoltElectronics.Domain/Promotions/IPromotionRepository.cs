namespace VoltElectronics.Domain.Promotions;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? exceptId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Every currently-active, in-window promotion with no code — the pool considered
    /// automatically for every cart/checkout and every catalog read, with no shopper action needed.</summary>
    Task<IReadOnlyList<Promotion>> GetActiveAutomaticAsync(CancellationToken cancellationToken = default);

    void Add(Promotion promotion);
    void Remove(Promotion promotion);
}

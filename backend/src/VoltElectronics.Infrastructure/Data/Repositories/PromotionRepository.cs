using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Promotions;

namespace VoltElectronics.Infrastructure.Data.Repositories;

internal sealed class PromotionRepository(AppDbContext db) : IPromotionRepository
{
    public Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Promotions.Include(p => p.Products).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = Promotion.Normalize(code);
        return db.Promotions.Include(p => p.Products).FirstOrDefaultAsync(p => p.Code == normalized, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(string code, int? exceptId = null, CancellationToken cancellationToken = default)
    {
        var normalized = Promotion.Normalize(code);
        return db.Promotions.AnyAsync(p => p.Code == normalized && p.Id != exceptId, cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Promotions.Include(p => p.Products)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Promotion>> GetActiveAutomaticAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await db.Promotions.Include(p => p.Products)
            .Where(p => p.Code == null && p.IsActive)
            .ToListAsync(cancellationToken);
        // Start/expiry are checked in memory (ValidateWindow) — a handful of rows, not worth a
        // brittle hand-rolled SQL date comparison.
        return candidates.Where(p => p.ValidateWindow() is null).ToList();
    }

    public void Add(Promotion promotion) => db.Promotions.Add(promotion);

    public void Remove(Promotion promotion) => db.Promotions.Remove(promotion);
}

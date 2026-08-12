using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Identity;

namespace VoltElectronics.Infrastructure.Data.Repositories;

internal sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);
}

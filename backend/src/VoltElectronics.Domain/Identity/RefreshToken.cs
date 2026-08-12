using VoltElectronics.Domain.Common;

namespace VoltElectronics.Domain.Identity;

/// <summary>Only the hash is stored — a leaked table can't be replayed against the API.</summary>
public sealed class RefreshToken : AggregateRoot
{
    private RefreshToken() { }

    public int Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    public static RefreshToken Issue(string userId, string tokenHash, DateTime expiresAt) =>
        new() { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt };

    public void Revoke() => RevokedAt = DateTime.UtcNow;
}

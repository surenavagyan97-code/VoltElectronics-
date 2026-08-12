namespace VoltElectronics.Domain.Identity;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    void Add(RefreshToken token);
}

namespace VoltElectronics.Application.Identity;

public record AuthUserDto(string Id, string Email, string FullName, IReadOnlyList<string> Roles);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, AuthUserDto User);

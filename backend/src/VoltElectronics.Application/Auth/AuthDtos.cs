namespace VoltElectronics.Application.Auth;

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);

public record AuthUserDto(string Id, string Email, string FullName, IReadOnlyList<string> Roles);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, AuthUserDto User);

public record AuthResult(bool Success, string? Error, AuthResponse? Data)
{
    public static AuthResult Ok(AuthResponse data) => new(true, null, data);
    public static AuthResult Fail(string error) => new(false, error, null);
}

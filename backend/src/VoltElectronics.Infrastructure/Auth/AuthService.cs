using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltElectronics.Application.Auth;
using VoltElectronics.Domain.Entities;
using VoltElectronics.Infrastructure.Data;
using VoltElectronics.Infrastructure.Identity;

namespace VoltElectronics.Infrastructure.Auth;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TokenService tokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return AuthResult.Fail("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return AuthResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, DbSeeder.CustomerRole);
        return AuthResult.Ok(await IssueTokensAsync(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return AuthResult.Fail("Invalid email or password.");

        return AuthResult.Ok(await IssueTokensAsync(user));
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var hash = TokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is null || !stored.IsActive)
            return AuthResult.Fail("Invalid or expired refresh token.");

        var user = await userManager.FindByIdAsync(stored.UserId);
        if (user is null)
            return AuthResult.Fail("Invalid or expired refresh token.");

        stored.RevokedAt = DateTime.UtcNow;
        return AuthResult.Ok(await IssueTokensAsync(user));
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = TokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is not null && stored.IsActive)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user, roles);

        var refreshToken = TokenService.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });
        await db.SaveChangesAsync();

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiresAt,
            new AuthUserDto(user.Id, user.Email ?? "", user.FullName ?? "", roles.ToList()));
    }
}

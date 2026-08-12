using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Application.Identity;
using VoltElectronics.Application.Identity.Commands;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Identity;
using VoltElectronics.Infrastructure.Auth;

namespace VoltElectronics.Infrastructure.Identity;

/// <summary>
/// Issues the access/refresh token pair every successful auth command answers with. The refresh
/// token itself leaves the API exactly once, here — only its hash is persisted.
/// </summary>
internal sealed class TokenIssuer(
    UserManager<ApplicationUser> users,
    TokenService tokens,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IOptions<JwtOptions> jwtOptions)
{
    public async Task<AuthResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await users.GetRolesAsync(user);
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user, roles);

        var refreshToken = TokenService.GenerateRefreshToken();
        refreshTokens.Add(RefreshToken.Issue(
            user.Id,
            TokenService.HashRefreshToken(refreshToken),
            DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiresAt,
            new AuthUserDto(user.Id, user.Email ?? "", user.FullName ?? "", roles.ToList()));
    }
}

internal sealed class RegisterHandler(
    UserManager<ApplicationUser> users,
    TokenIssuer issuer) : ICommandHandler<RegisterCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (await users.FindByEmailAsync(command.Email) is not null)
            return Error.Invalid("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            FullName = command.FullName
        };
        var result = await users.CreateAsync(user, command.Password);
        if (!result.Succeeded)
            return Error.Invalid(string.Join(" ", result.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(user, Roles.Customer);
        return await issuer.IssueAsync(user, cancellationToken);
    }
}

internal sealed class LoginHandler(
    UserManager<ApplicationUser> users,
    TokenIssuer issuer) : ICommandHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(command.Email);
        if (user is null || !await users.CheckPasswordAsync(user, command.Password))
            return Error.Unauthorized("Invalid email or password.");

        return await issuer.IssueAsync(user, cancellationToken);
    }
}

internal sealed class RefreshSessionHandler(
    UserManager<ApplicationUser> users,
    IRefreshTokenRepository refreshTokens,
    TokenIssuer issuer) : ICommandHandler<RefreshSessionCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> HandleAsync(
        RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var stored = await refreshTokens.GetByHashAsync(
            TokenService.HashRefreshToken(command.RefreshToken), cancellationToken);
        if (stored is null || !stored.IsActive)
            return Error.Unauthorized("Invalid or expired refresh token.");

        var user = await users.FindByIdAsync(stored.UserId);
        if (user is null)
            return Error.Unauthorized("Invalid or expired refresh token.");

        // Rotation: the presented token dies with the exchange; IssueAsync saves both changes.
        stored.Revoke();
        return await issuer.IssueAsync(user, cancellationToken);
    }
}

internal sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork) : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var stored = await refreshTokens.GetByHashAsync(
            TokenService.HashRefreshToken(command.RefreshToken), cancellationToken);
        if (stored is not null && stored.IsActive)
        {
            stored.Revoke();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Result.Success();
    }
}

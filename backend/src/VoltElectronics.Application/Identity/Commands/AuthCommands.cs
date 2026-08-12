using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;

namespace VoltElectronics.Application.Identity.Commands;

// These records double as the request bodies — the property names are the wire contract, so there's
// nothing for a separate DTO to add. Their handlers live in the persistence layer, next to the
// ASP.NET Identity stores and the token signing key they can't work without.

/// <summary>Creates a Customer account and signs the shopper straight in.</summary>
public sealed record RegisterCommand(string Email, string Password, string FullName) : ICommand<Result<AuthResponse>>;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<AuthResponse>>;

/// <summary>Trades a valid refresh token for a new token pair, revoking the one presented.</summary>
public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<Result<AuthResponse>>;

/// <summary>Revokes a refresh token. Succeeds even if the token was already unusable.</summary>
public sealed record LogoutCommand(string RefreshToken) : ICommand<Result>;

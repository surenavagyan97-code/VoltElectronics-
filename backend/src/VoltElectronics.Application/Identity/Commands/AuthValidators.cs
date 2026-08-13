using FluentValidation;

namespace VoltElectronics.Application.Identity.Commands;

internal sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        // Mirrors the Identity password policy (RequiredLength = 8) so obvious rejects don't
        // have to travel all the way to UserManager.
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(150);
    }
}

/// <summary>Presence only — anything specific about what's wrong would help an attacker.</summary>
internal sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
    }
}

internal sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator() => RuleFor(c => c.RefreshToken).NotEmpty();
}

// LogoutCommand deliberately has no validator: it succeeds even for unusable tokens, and an
// empty one is just a no-op sign-out, not a request worth rejecting.

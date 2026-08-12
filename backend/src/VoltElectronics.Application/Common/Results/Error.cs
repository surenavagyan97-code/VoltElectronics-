namespace VoltElectronics.Application.Common.Results;

/// <summary>
/// How the API should surface a failed command. Deliberately coarse — these three map one-to-one
/// onto the status codes the storefront already handles.
/// </summary>
public enum ErrorKind
{
    /// <summary>The request was understood but rejected — 400 with an <c>{ error }</c> body.</summary>
    Invalid,
    /// <summary>Bad or expired credentials — 401.</summary>
    Unauthorized,
    /// <summary>404. Used for reads only; failed mutations report <see cref="Invalid"/> so the
    /// storefront's single "read .error off a 400" path keeps working.</summary>
    NotFound
}

public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error Invalid(string message) => new(ErrorKind.Invalid, message);
    public static Error Unauthorized(string message) => new(ErrorKind.Unauthorized, message);
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
}

namespace VoltElectronics.Application.Common.Results;

/// <summary>
/// Outcome of a command that can fail for reasons the caller is expected to handle. Rule violations
/// an aggregate catches for itself throw <see cref="Domain.Common.DomainException"/> instead — this
/// type is for the checks a handler has to make before the aggregate is even reachable.
/// </summary>
public readonly record struct Result
{
    private Result(Error? error) => Error = error;

    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public readonly record struct Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

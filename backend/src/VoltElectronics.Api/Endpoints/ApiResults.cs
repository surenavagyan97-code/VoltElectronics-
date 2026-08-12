using VoltElectronics.Application.Common.Results;

namespace VoltElectronics.Api.Endpoints;

/// <summary>
/// One place that decides how a failed command reads over the wire: the storefront expects a
/// <c>{ error }</c> body on 400/401 and an empty 404.
/// </summary>
internal static class ApiResults
{
    /// <summary>200 with the value, or the error mapped to its status code.</summary>
    public static IResult Ok<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Fail(result.Error!);

    /// <summary>204, or the error mapped to its status code.</summary>
    public static IResult NoContent(Result result) =>
        result.IsSuccess ? Results.NoContent() : Fail(result.Error!);

    public static IResult Fail(Error error) => error.Kind switch
    {
        ErrorKind.Unauthorized => Results.Json(new { error = error.Message }, statusCode: StatusCodes.Status401Unauthorized),
        ErrorKind.NotFound => Results.NotFound(),
        _ => Results.BadRequest(new { error = error.Message })
    };
}

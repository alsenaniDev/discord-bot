namespace DiscordBot.Shared;

public sealed record OperationResult<T>(T? Value, string? Error, int StatusCode = 200, string? Code = null, string? Feature = null)
{
    public bool Succeeded => Error is null;
    public static OperationResult<T> Ok(T value) => new(value, null);
    public static OperationResult<T> Fail(string error, int statusCode = 400) => new(default, error, statusCode);
    public static OperationResult<T> Fail(string error, int statusCode, string code, string? feature = null) => new(default, error, statusCode, code, feature);
}

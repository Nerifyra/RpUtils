namespace RpUtils.Services;

public readonly record struct Result
{
    public string? Error { get; init; }
    public bool Success => Error is null;

    public static Result Ok() => new();
    public static Result Fail(string error) => new() { Error = error };
}

public readonly record struct Result<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }
    public bool Success => Error is null;

    public static Result<T> Ok(T value) => new() { Value = value };
    public static Result<T> Fail(string error) => new() { Error = error };
}

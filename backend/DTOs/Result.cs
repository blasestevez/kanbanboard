namespace Trellochocero.Api.DTOs;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }
    public IDictionary<string, string[]>? ValidationErrors { get; private set; }

    private Result() { }

    public static Result<T> Success(T value, int statusCode = 200) =>
        new() { IsSuccess = true, Value = value, StatusCode = statusCode };

    public static Result<T> Created(T value) =>
        new() { IsSuccess = true, Value = value, StatusCode = 201 };

    public static Result<T> Failure(string error, int statusCode = 400, IDictionary<string, string[]>? errors = null) =>
        new() { IsSuccess = false, Error = error, StatusCode = statusCode, ValidationErrors = errors };
}

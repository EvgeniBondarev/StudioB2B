namespace StudioB2B.Infrastructure.Integrations.Ozon;

public class OzonApiResult<T>
{
    public bool IsSuccess { get; init; }

    public T? Data { get; init; }

    public int? StatusCode { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static OzonApiResult<T> Success(T data) =>
        new()
        {
            IsSuccess = true,
            Data = data
        };

    public static OzonApiResult<T> Failure(
        int? statusCode,
        string? message,
        string? errorCode = null) =>
        new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = message,
            ErrorCode = errorCode
        };
}


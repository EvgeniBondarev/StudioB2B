namespace StudioB2B.Shared.DTOs;

public class OzonApiResultDto<T>
{
    public bool IsSuccess { get; init; }

    public T? Data { get; init; }

    public int? StatusCode { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static OzonApiResultDto<T> Success(T data)
    {
        return new()
               {
                   IsSuccess = true,
                   Data = data
               };
    }

    public static OzonApiResultDto<T> Failure(int? statusCode, string? message, string? errorCode = null)
    {
        return new()
               {
                   IsSuccess = false,
                   StatusCode = statusCode,
                   ErrorMessage = message,
                   ErrorCode = errorCode
               };
    }
}

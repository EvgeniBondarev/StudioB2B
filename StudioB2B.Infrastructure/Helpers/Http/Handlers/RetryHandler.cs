using System.Net;
using Microsoft.Extensions.Logging;

namespace StudioB2B.Infrastructure.Helpers.Http.Handlers;

public class RetryHandler : DelegatingHandler
{
    private const int MaxRetries = 3;
    private readonly ILogger<RetryHandler> _logger;

    public RetryHandler(ILogger<RetryHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return response;

            if (ShouldRetry(response.StatusCode) && attempt < MaxRetries)
            {
                var delay = GetRetryDelay(response, attempt);
                _logger.LogWarning("Retry attempt {Attempt} for {Method} {Uri} after {DelayMs}ms (status {Status})",
                                   attempt + 1, request.Method, request.RequestUri, delay.TotalMilliseconds, (int)response.StatusCode);

                response.Dispose();
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            return response;
        }

        throw new HttpRequestException("Max retry attempts exceeded");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            (HttpStatusCode)429;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta;

        return TimeSpan.FromSeconds(Math.Pow(2, attempt));
    }
}

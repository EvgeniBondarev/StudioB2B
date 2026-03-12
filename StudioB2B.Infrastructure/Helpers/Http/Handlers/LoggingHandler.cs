using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace StudioB2B.Infrastructure.Helpers.Http.Handlers;

public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("Outgoing {Method} {Uri}", request.Method, request.RequestUri);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            _logger.LogDebug("Response {Method} {Uri} {Status} {ElapsedMs}ms",
                             request.Method, request.RequestUri, (int)response.StatusCode, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed {Method} {Uri} after {ElapsedMs}ms",
                             request.Method, request.RequestUri, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

namespace StudioB2B.Infrastructure.Helpers.Http.Handlers;

public class RateLimitHandler : DelegatingHandler
{
    private readonly SemaphoreSlim _semaphore = new(5);
    private DateTime _lastRequest = DateTime.MinValue;
    private readonly TimeSpan _minDelay = TimeSpan.FromMilliseconds(100);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var delay = _minDelay - (DateTime.UtcNow - _lastRequest);
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);

            _lastRequest = DateTime.UtcNow;

            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

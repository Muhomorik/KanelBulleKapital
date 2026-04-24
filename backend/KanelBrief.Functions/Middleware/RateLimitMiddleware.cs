using System.Net;
using System.Text.Json;
using KanelBrief.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Middleware;

/// <summary>
/// Per-IP sliding-window rate limit for anonymous HTTP endpoints. Returns
/// 429 with a <c>Retry-After</c> header when a caller exceeds the limit.
/// Skips <c>Sync*</c> functions (already bearer-protected) and non-HTTP
/// triggers (timers, queues).
/// </summary>
public class RateLimitMiddleware : IFunctionsWorkerMiddleware
{
    private const string SkipPrefix = "Sync";
    private const string ForwardedForHeader = "X-Forwarded-For";
    private const string UnknownIp = "unknown";

    // 100 req/min per IP: ~20x normal dashboard traffic, still caps a runaway
    // script to 1.67 req/s — bounded enough that Azure daily compute quota
    // and budget alerts handle the remainder.
    internal static readonly TimeSpan Window = TimeSpan.FromSeconds(60);
    internal const int RequestsPerWindow = 100;

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _time;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(IMemoryCache cache, TimeProvider time, ILogger<RateLimitMiddleware> logger)
    {
        _cache = cache;
        _time = time;
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (ShouldSkip(context.FunctionDefinition.Name))
        {
            await next(context);
            return;
        }

        var req = await context.GetHttpRequestDataAsync();
        if (req is null)
        {
            await next(context);
            return;
        }

        var forwardedFor = req.Headers.TryGetValues(ForwardedForHeader, out var values)
            ? values.FirstOrDefault()
            : null;
        var ip = ExtractClientIp(forwardedFor);
        var now = _time.GetUtcNow();

        if (RecordAndCheck(ip, now))
        {
            _logger.LogWarning("Rate limit exceeded for {Ip} on {Function}",
                ip, context.FunctionDefinition.Name);
            await ShortCircuit(req, context);
            return;
        }

        await next(context);
    }

    /// <summary>Opt-out: <c>Sync</c>-prefixed functions are gated by bearer auth, not rate limit.</summary>
    internal static bool ShouldSkip(string functionName)
        => functionName.StartsWith(SkipPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Parses the client IP out of an <c>X-Forwarded-For</c> header value.
    /// Azure's frontend sets this to <c>"client, proxy1, proxy2"</c>; the first
    /// entry is the originating caller.
    /// </summary>
    internal static string ExtractClientIp(string? xForwardedFor)
    {
        if (string.IsNullOrWhiteSpace(xForwardedFor))
            return UnknownIp;

        var comma = xForwardedFor.IndexOf(',');
        var first = comma > 0 ? xForwardedFor[..comma] : xForwardedFor;
        var trimmed = first.Trim();
        return string.IsNullOrEmpty(trimmed) ? UnknownIp : trimmed;
    }

    /// <summary>
    /// Pure predicate: drops entries older than <paramref name="window"/>,
    /// returns <c>true</c> if <paramref name="timestamps"/> already holds
    /// <paramref name="limit"/> entries; otherwise appends <paramref name="now"/>
    /// and returns <c>false</c>.
    /// </summary>
    internal static bool IsOverLimit(
        Queue<DateTimeOffset> timestamps,
        DateTimeOffset now,
        TimeSpan window,
        int limit)
    {
        var cutoff = now - window;
        while (timestamps.Count > 0 && timestamps.Peek() < cutoff)
            timestamps.Dequeue();

        if (timestamps.Count >= limit)
            return true;

        timestamps.Enqueue(now);
        return false;
    }

    private bool RecordAndCheck(string ip, DateTimeOffset now)
    {
        var timestamps = _cache.GetOrCreate(CacheKey(ip), entry =>
        {
            // Evict inactive IPs after 2x the window so the cache self-cleans.
            entry.SlidingExpiration = Window + Window;
            return new Queue<DateTimeOffset>();
        })!;

        lock (timestamps)
        {
            return IsOverLimit(timestamps, now, Window, RequestsPerWindow);
        }
    }

    private static string CacheKey(string ip) => $"ratelimit:{ip}";

    private static async Task ShortCircuit(HttpRequestData req, FunctionContext ctx)
    {
        var resp = req.CreateResponse(HttpStatusCode.TooManyRequests);
        resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
        resp.Headers.Add("Retry-After", ((int)Window.TotalSeconds).ToString());
        await resp.WriteStringAsync(JsonSerializer.Serialize(
            new { error = "Too many requests" }, KanelJsonOptions.CamelCase));
        ctx.GetInvocationResult().Value = resp;
    }
}

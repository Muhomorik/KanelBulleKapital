using System.Net;
using System.Text.Json;
using KanelBrief.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Middleware;

/// <summary>
/// Gates functions whose name starts with <c>"Sync"</c> behind a symmetric bearer token.
/// All other functions bypass this middleware. Logs never include the raw header or token.
/// </summary>
public class BearerTokenAuthMiddleware : IFunctionsWorkerMiddleware
{
    private const string OptInPrefix = "Sync";
    private const string BearerScheme = "Bearer ";

    private readonly IConfiguration _config;
    private readonly ILogger<BearerTokenAuthMiddleware> _logger;

    public BearerTokenAuthMiddleware(IConfiguration config, ILogger<BearerTokenAuthMiddleware> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (!RequiresAuth(context.FunctionDefinition.Name))
        {
            await next(context);
            return;
        }

        var expected = _config["SYNC_AUTH_TOKEN"];
        if (string.IsNullOrEmpty(expected))
        {
            _logger.LogCritical("SYNC_AUTH_TOKEN is not configured; sync endpoints fail closed");
            await ShortCircuit(context, HttpStatusCode.ServiceUnavailable, "Sync endpoints are not configured");
            return;
        }

        var req = await context.GetHttpRequestDataAsync();
        if (req is null)
        {
            await next(context);
            return;
        }

        var authHeader = req.Headers.TryGetValues("Authorization", out var values)
            ? values.FirstOrDefault()
            : null;

        if (!IsBearerValid(authHeader, expected))
        {
            _logger.LogWarning("Sync auth rejected for function {Function}", context.FunctionDefinition.Name);
            await ShortCircuit(context, HttpStatusCode.Unauthorized, "Unauthorized");
            return;
        }

        await next(context);
    }

    /// <summary>Opt-in: any function whose name starts with <c>Sync</c> requires a bearer token.</summary>
    internal static bool RequiresAuth(string functionName)
        => functionName.StartsWith(OptInPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Pure predicate: does <paramref name="authHeader"/> carry a <c>Bearer</c> token equal to
    /// <paramref name="expectedToken"/>? Comparison is constant-time with respect to the token value.
    /// </summary>
    internal static bool IsBearerValid(string? authHeader, string expectedToken)
    {
        if (string.IsNullOrEmpty(authHeader)
            || !authHeader.StartsWith(BearerScheme, StringComparison.Ordinal))
            return false;

        return FixedTimeEquals(authHeader.AsSpan(BearerScheme.Length), expectedToken);
    }

    private static async Task ShortCircuit(FunctionContext ctx, HttpStatusCode status, string body)
    {
        var req = await ctx.GetHttpRequestDataAsync();
        if (req is null) return;

        var resp = req.CreateResponse(status);
        resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await resp.WriteStringAsync(JsonSerializer.Serialize(new { error = body }, KanelJsonOptions.CamelCase));
        ctx.GetInvocationResult().Value = resp;
    }

    private static bool FixedTimeEquals(ReadOnlySpan<char> a, string b)
    {
        if (a.Length != b.Length) return false;
        var result = 0;
        for (var i = 0; i < a.Length; i++) result |= a[i] ^ b[i];
        return result == 0;
    }
}

using System.Net;
using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using KanelBrief.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Api;

/// <summary>
/// Date-range sync API consumed by the FikaForecast desktop app.
/// </summary>
/// <remarks>
/// Function names MUST begin with <c>"Sync"</c> — <see cref="Middleware.BearerTokenAuthMiddleware"/>
/// opts in by that prefix. All four endpoints are <see cref="AuthorizationLevel.Anonymous"/>; the
/// middleware enforces the bearer token before the function body runs. Responses use
/// <see cref="KanelJsonOptions.CamelCase"/> so the wire contract matches
/// <c>FikaForecast.Application.Sync.Dtos.SyncRunsResponse&lt;T&gt;</c>.
/// Exception messages are never echoed to clients — <c>ex.Message</c> could contain header
/// values. The static string "Internal server error" is the only 500 body.
/// </remarks>
public class SyncApi
{
    private readonly ILogger<SyncApi> _logger;
    private readonly IAgentRunRepository _repository;

    public SyncApi(ILogger<SyncApi> logger, IAgentRunRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("SyncNewsBriefRuns")]
    public async Task<HttpResponseData> SyncNewsBriefRuns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sync/news-briefs")] HttpRequestData req)
    {
        if (!TryParseRange(req.Query["from"], req.Query["to"], out var from, out var to, out var error))
            return await WriteJsonAsync(req, new { error }, HttpStatusCode.BadRequest);

        try
        {
            var runs = await _repository.GetNewsBriefRunsByDateRangeAsync(from, to);
            _logger.LogInformation("Sync: fetched {Count} news-brief runs for {From}..{To}", runs.Count, from, to);
            return await WriteJsonAsync(req, new SyncRunsResponse<NewsBriefRun>
            {
                From = from,
                To = to,
                Count = runs.Count,
                Runs = runs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncNewsBriefRuns failed");
            return await WriteJsonAsync(req, new { error = "Internal server error" }, HttpStatusCode.InternalServerError);
        }
    }

    [Function("SyncWeeklySummaryRuns")]
    public async Task<HttpResponseData> SyncWeeklySummaryRuns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sync/weekly-summaries")] HttpRequestData req)
    {
        if (!TryParseRange(req.Query["from"], req.Query["to"], out var from, out var to, out var error))
            return await WriteJsonAsync(req, new { error }, HttpStatusCode.BadRequest);

        try
        {
            var runs = await _repository.GetWeeklySummaryRunsByDateRangeAsync(from, to);
            _logger.LogInformation("Sync: fetched {Count} weekly-summary runs for {From}..{To}", runs.Count, from, to);
            return await WriteJsonAsync(req, new SyncRunsResponse<WeeklySummaryRun>
            {
                From = from,
                To = to,
                Count = runs.Count,
                Runs = runs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncWeeklySummaryRuns failed");
            return await WriteJsonAsync(req, new { error = "Internal server error" }, HttpStatusCode.InternalServerError);
        }
    }

    [Function("SyncSubstitutionChainRuns")]
    public async Task<HttpResponseData> SyncSubstitutionChainRuns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sync/substitution-chains")] HttpRequestData req)
    {
        if (!TryParseRange(req.Query["from"], req.Query["to"], out var from, out var to, out var error))
            return await WriteJsonAsync(req, new { error }, HttpStatusCode.BadRequest);

        try
        {
            var runs = await _repository.GetSubstitutionChainRunsByDateRangeAsync(from, to);
            _logger.LogInformation("Sync: fetched {Count} substitution-chain runs for {From}..{To}", runs.Count, from, to);
            return await WriteJsonAsync(req, new SyncRunsResponse<SubstitutionChainRun>
            {
                From = from,
                To = to,
                Count = runs.Count,
                Runs = runs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncSubstitutionChainRuns failed");
            return await WriteJsonAsync(req, new { error = "Internal server error" }, HttpStatusCode.InternalServerError);
        }
    }

    [Function("SyncOpportunityScanRuns")]
    public async Task<HttpResponseData> SyncOpportunityScanRuns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sync/opportunity-scans")] HttpRequestData req)
    {
        if (!TryParseRange(req.Query["from"], req.Query["to"], out var from, out var to, out var error))
            return await WriteJsonAsync(req, new { error }, HttpStatusCode.BadRequest);

        try
        {
            var runs = await _repository.GetOpportunityScanRunsByDateRangeAsync(from, to);
            _logger.LogInformation("Sync: fetched {Count} opportunity-scan runs for {From}..{To}", runs.Count, from, to);
            return await WriteJsonAsync(req, new SyncRunsResponse<OpportunityScanRun>
            {
                From = from,
                To = to,
                Count = runs.Count,
                Runs = runs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncOpportunityScanRuns failed");
            return await WriteJsonAsync(req, new { error = "Internal server error" }, HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>Parses raw query values for <c>from</c>/<c>to</c>. Error strings never echo user input.</summary>
    internal static bool TryParseRange(string? fromRaw, string? toRaw, out string from, out string to, out string error)
    {
        from = fromRaw ?? "";
        to = toRaw ?? "";
        error = "";

        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var f))
        {
            error = "Invalid 'from' — expected yyyy-MM-dd";
            return false;
        }
        if (!DateOnly.TryParseExact(to, "yyyy-MM-dd", out var t))
        {
            error = "Invalid 'to' — expected yyyy-MM-dd";
            return false;
        }
        if (f > t)
        {
            error = "'from' must be <= 'to'";
            return false;
        }
        return true;
    }

    private static async Task<HttpResponseData> WriteJsonAsync<T>(
        HttpRequestData req, T payload, HttpStatusCode status = HttpStatusCode.OK)
    {
        var response = req.CreateResponse(status);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(payload, KanelJsonOptions.CamelCase));
        return response;
    }
}

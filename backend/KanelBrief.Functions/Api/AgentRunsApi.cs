using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Api;

/// <summary>
/// Agent Runs API: retrieve historical agent run results.
/// Exposes endpoints to fetch runs by date, type, and ID.
/// </summary>
public class AgentRunsApi
{
    private readonly ILogger<AgentRunsApi> _logger;
    private readonly IAgentRunRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentRunsApi(
        ILogger<AgentRunsApi> logger,
        IAgentRunRepository repository)
    {
        _logger = logger;
        _repository = repository;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    /// <summary>
    /// Composite dashboard endpoint: returns all run types with pre-computed metadata.
    /// Eliminates the need for clients to make 4 separate calls and null-coalesce across them.
    /// </summary>
    [Function("GetDashboard")]
    public async Task<HttpResponseData> GetDashboard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard")] HttpRequestData req)
    {
        try
        {
            var runDate = req.Query["date"];

            // Fetch all run types in parallel
            var newsBriefTask = string.IsNullOrEmpty(runDate)
                ? FindLatestAsync(_repository.GetNewsBriefRunsByDateAsync)
                : _repository.GetNewsBriefRunsByDateAsync(runDate);
            var weeklySummaryTask = string.IsNullOrEmpty(runDate)
                ? FindLatestAsync(_repository.GetWeeklySummaryRunsByDateAsync)
                : _repository.GetWeeklySummaryRunsByDateAsync(runDate);
            var substitutionChainTask = string.IsNullOrEmpty(runDate)
                ? FindLatestAsync(_repository.GetSubstitutionChainRunsByDateAsync)
                : _repository.GetSubstitutionChainRunsByDateAsync(runDate);
            var opportunityScanTask = string.IsNullOrEmpty(runDate)
                ? FindLatestAsync(_repository.GetOpportunityScanRunsByDateAsync)
                : _repository.GetOpportunityScanRunsByDateAsync(runDate);

            await Task.WhenAll(newsBriefTask, weeklySummaryTask, substitutionChainTask, opportunityScanTask);

            var newsBrief = newsBriefTask.Result.FirstOrDefault();
            var weeklySummary = weeklySummaryTask.Result.FirstOrDefault();
            var substitutionChain = substitutionChainTask.Result.FirstOrDefault();
            var opportunityScan = opportunityScanTask.Result.FirstOrDefault();

            var dashboard = new DashboardResponse
            {
                RunDate = newsBrief?.RunDate
                    ?? weeklySummary?.RunDate
                    ?? substitutionChain?.RunDate
                    ?? opportunityScan?.RunDate,
                HasData = newsBrief != null || weeklySummary != null
                    || substitutionChain != null || opportunityScan != null,
                NewsBrief = newsBrief,
                WeeklySummary = weeklySummary,
                SubstitutionChain = substitutionChain,
                OpportunityScan = opportunityScan
            };

            _logger.LogInformation("Dashboard fetched: HasData={HasData}, RunDate={RunDate}",
                dashboard.HasData, dashboard.RunDate ?? "none");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(dashboard);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDashboard failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>
    /// Get News Brief runs.
    /// If ?date is provided, returns runs for that date.
    /// Otherwise, returns the latest available (up to 7 days back).
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("GetNewsBriefRuns")]
    public async Task<HttpResponseData> GetNewsBriefRuns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runs/news-briefs")] HttpRequestData req)
    {
        try
        {
            var runDate = req.Query["date"];
            var runs = string.IsNullOrEmpty(runDate)
                ? await FindLatestAsync(_repository.GetNewsBriefRunsByDateAsync)
                : await _repository.GetNewsBriefRunsByDateAsync(runDate);

            _logger.LogInformation("Fetched {Count} News Brief runs (date={Date})", runs.Count, runDate ?? "latest");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(runs);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetNewsBriefRuns failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>Get a specific News Brief run by date and ID.</summary>
    [Function("GetNewsBriefRun")]
    public async Task<HttpResponseData> GetNewsBriefRun(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runs/news-briefs/{runDate}/{runId}")] HttpRequestData req,
        string runDate,
        string runId)
    {
        try
        {
            _logger.LogInformation("Fetching News Brief run {RunDate}/{RunId}", runDate, runId);

            var run = await _repository.GetNewsBriefRunAsync(runDate, runId);
            if (run == null)
                return req.CreateResponse(System.Net.HttpStatusCode.NotFound);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(run);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetNewsBriefRun failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>Get Weekly Summary runs. If ?date is provided, returns runs for that date. Otherwise returns the latest available (up to 7 days back).</summary>
    [Function("GetWeeklySummaryRuns")]
    public async Task<HttpResponseData> GetWeeklySummaryRuns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runs/weekly-summaries")] HttpRequestData req)
    {
        try
        {
            var runDate = req.Query["date"];
            var runs = string.IsNullOrEmpty(runDate)
                ? await FindLatestAsync(_repository.GetWeeklySummaryRunsByDateAsync)
                : await _repository.GetWeeklySummaryRunsByDateAsync(runDate);

            _logger.LogInformation("Fetched {Count} Weekly Summary runs (date={Date})", runs.Count, runDate ?? "latest");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(runs);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetWeeklySummaryRuns failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>Get a specific Weekly Summary run by date and ID.</summary>
    [Function("GetWeeklySummaryRun")]
    public async Task<HttpResponseData> GetWeeklySummaryRun(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runs/weekly-summaries/{runDate}/{runId}")] HttpRequestData req,
        string runDate,
        string runId)
    {
        try
        {
            _logger.LogInformation("Fetching Weekly Summary run {RunDate}/{RunId}", runDate, runId);

            var run = await _repository.GetWeeklySummaryRunAsync(runDate, runId);
            if (run == null)
                return req.CreateResponse(System.Net.HttpStatusCode.NotFound);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(run);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetWeeklySummaryRun failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>Get Substitution Chain runs. If ?date is provided, returns runs for that date. Otherwise returns the latest available (up to 7 days back).</summary>
    [Function("GetSubstitutionChainRuns")]
    public async Task<HttpResponseData> GetSubstitutionChainRuns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runs/substitution-chains")] HttpRequestData req)
    {
        try
        {
            var runDate = req.Query["date"];
            var runs = string.IsNullOrEmpty(runDate)
                ? await FindLatestAsync(_repository.GetSubstitutionChainRunsByDateAsync)
                : await _repository.GetSubstitutionChainRunsByDateAsync(runDate);

            _logger.LogInformation("Fetched {Count} Substitution Chain runs (date={Date})", runs.Count, runDate ?? "latest");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(runs);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSubstitutionChainRuns failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>Get a specific Substitution Chain run by date and ID.</summary>
    [Function("GetSubstitutionChainRun")]
    public async Task<HttpResponseData> GetSubstitutionChainRun(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runs/substitution-chains/{runDate}/{runId}")] HttpRequestData req,
        string runDate,
        string runId)
    {
        try
        {
            _logger.LogInformation("Fetching Substitution Chain run {RunDate}/{RunId}", runDate, runId);

            var run = await _repository.GetSubstitutionChainRunAsync(runDate, runId);
            if (run == null)
                return req.CreateResponse(System.Net.HttpStatusCode.NotFound);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(run);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSubstitutionChainRun failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>Get Opportunity Scan runs. If ?date is provided, returns runs for that date. Otherwise returns the latest available (up to 7 days back).</summary>
    [Function("GetOpportunityScanRuns")]
    public async Task<HttpResponseData> GetOpportunityScanRuns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runs/opportunity-scans")] HttpRequestData req)
    {
        try
        {
            var runDate = req.Query["date"];
            var runs = string.IsNullOrEmpty(runDate)
                ? await FindLatestAsync(_repository.GetOpportunityScanRunsByDateAsync)
                : await _repository.GetOpportunityScanRunsByDateAsync(runDate);

            _logger.LogInformation("Fetched {Count} Opportunity Scan runs (date={Date})", runs.Count, runDate ?? "latest");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(runs);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOpportunityScanRuns failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>Get a specific Opportunity Scan run by date and ID.</summary>
    [Function("GetOpportunityScanRun")]
    public async Task<HttpResponseData> GetOpportunityScanRun(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runs/opportunity-scans/{runDate}/{runId}")] HttpRequestData req,
        string runDate,
        string runId)
    {
        try
        {
            _logger.LogInformation("Fetching Opportunity Scan run {RunDate}/{RunId}", runDate, runId);

            var run = await _repository.GetOpportunityScanRunAsync(runDate, runId);
            if (run == null)
                return req.CreateResponse(System.Net.HttpStatusCode.NotFound);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(run);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOpportunityScanRun failed");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>Scan backwards from today up to 7 days to find the latest available runs.</summary>
    internal static async Task<List<T>> FindLatestAsync<T>(Func<string, Task<List<T>>> getByDate)
    {
        var today = DateTimeOffset.UtcNow;
        for (var i = 0; i < 7; i++)
        {
            var date = today.AddDays(-i).ToString("yyyy-MM-dd");
            var runs = await getByDate(date);
            if (runs.Count > 0)
                return runs;
        }
        return [];
    }
}

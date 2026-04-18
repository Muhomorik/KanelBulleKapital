using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using FikaForecast.Application.Sync;
using FikaForecast.Application.Sync.Dtos;

namespace FikaForecast.Infrastructure.Sync;

/// <summary>
/// Fetches agent runs from the backend <c>/api/sync/*</c> endpoints over HTTP.
/// Builds a fresh <see cref="HttpRequestMessage"/> per call — never mutates
/// <see cref="HttpClient.DefaultRequestHeaders"/>.
/// </summary>
public class AgentRunSyncClient : IAgentRunSyncClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;

    public AgentRunSyncClient(HttpClient http)
    {
        _http = http;
    }

    public Task<List<SyncNewsBriefRun>> FetchNewsBriefRunsAsync(
        SyncConnectionInfo connection, DateOnly from, DateOnly to, CancellationToken ct)
        => FetchRunsAsync<SyncNewsBriefRun>(connection, "news-briefs", from, to, ct);

    public Task<List<SyncWeeklySummaryRun>> FetchWeeklySummaryRunsAsync(
        SyncConnectionInfo connection, DateOnly from, DateOnly to, CancellationToken ct)
        => FetchRunsAsync<SyncWeeklySummaryRun>(connection, "weekly-summaries", from, to, ct);

    public Task<List<SyncSubstitutionChainRun>> FetchSubstitutionChainRunsAsync(
        SyncConnectionInfo connection, DateOnly from, DateOnly to, CancellationToken ct)
        => FetchRunsAsync<SyncSubstitutionChainRun>(connection, "substitution-chains", from, to, ct);

    public Task<List<SyncOpportunityScanRun>> FetchOpportunityScanRunsAsync(
        SyncConnectionInfo connection, DateOnly from, DateOnly to, CancellationToken ct)
        => FetchRunsAsync<SyncOpportunityScanRun>(connection, "opportunity-scans", from, to, ct);

    private async Task<List<T>> FetchRunsAsync<T>(
        SyncConnectionInfo connection, string segment, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var url = $"{connection.BaseUrl.TrimEnd('/')}/api/sync/{segment}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AuthToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new SyncTransportException($"Failed to reach sync server: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new SyncTransportException("Sync request timed out.", ex);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new SyncAuthException();

        if (!response.IsSuccessStatusCode)
            throw new SyncTransportException($"Sync server returned {(int)response.StatusCode} {response.ReasonPhrase}.");

        var envelope = await JsonSerializer.DeserializeAsync<SyncRunsResponse<T>>(
            await response.Content.ReadAsStreamAsync(ct), JsonOptions, ct);

        return envelope?.Runs ?? [];
    }
}

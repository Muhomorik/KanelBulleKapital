using System.Diagnostics;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Sync;
using FikaForecast.Application.Sync.Mappers;
using NLog;

namespace FikaForecast.Infrastructure.Sync;

/// <summary>
/// Orchestrates pulling agent runs from the backend and inserting into local SQLite.
/// Processes types in FK order: NewsBrief → WeeklySummary → SubstitutionChain → OpportunityScan.
/// </summary>
public class SyncService : ISyncService
{
    private readonly IAgentRunSyncClient _client;
    private readonly SyncRunMapper _mapper;
    private readonly INewsBriefRunRepository _newsBriefRepo;
    private readonly IWeeklySummaryRunRepository _weeklySummaryRepo;
    private readonly ISubstitutionChainRunRepository _substitutionChainRepo;
    private readonly IOpportunityScanRunRepository _opportunityScanRepo;
    private readonly ILogger _logger;

    public SyncService(
        IAgentRunSyncClient client,
        SyncRunMapper mapper,
        INewsBriefRunRepository newsBriefRepo,
        IWeeklySummaryRunRepository weeklySummaryRepo,
        ISubstitutionChainRunRepository substitutionChainRepo,
        IOpportunityScanRunRepository opportunityScanRepo,
        ILogger logger)
    {
        _client = client;
        _mapper = mapper;
        _newsBriefRepo = newsBriefRepo;
        _weeklySummaryRepo = weeklySummaryRepo;
        _substitutionChainRepo = substitutionChainRepo;
        _opportunityScanRepo = opportunityScanRepo;
        _logger = logger;
    }

    public async Task<SyncResult> RunAsync(
        SyncConnectionInfo connection,
        SyncRange range,
        IProgress<SyncProgressUpdate>? progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var (from, to) = range.ToDateRange();
        int totalFetched = 0, totalInserted = 0, totalSkipped = 0, totalFailed = 0;

        try
        {
            // FK order: NewsBrief → WeeklySummary → SubstitutionChain → OpportunityScan
            var (f, i, s, fail) = await SyncNewsBriefsAsync(connection, from, to, progress, ct);
            totalFetched += f; totalInserted += i; totalSkipped += s; totalFailed += fail;

            (f, i, s, fail) = await SyncWeeklySummariesAsync(connection, from, to, progress, ct);
            totalFetched += f; totalInserted += i; totalSkipped += s; totalFailed += fail;

            (f, i, s, fail) = await SyncSubstitutionChainsAsync(connection, from, to, progress, ct);
            totalFetched += f; totalInserted += i; totalSkipped += s; totalFailed += fail;

            (f, i, s, fail) = await SyncOpportunityScansAsync(connection, from, to, progress, ct);
            totalFetched += f; totalInserted += i; totalSkipped += s; totalFailed += fail;

            return new SyncResult(totalFetched, totalInserted, totalSkipped, totalFailed, sw.ElapsedMilliseconds);
        }
        catch (SyncAuthException ex)
        {
            _logger.Warn(ex, "Sync authentication failed");
            return new SyncResult(totalFetched, totalInserted, totalSkipped, totalFailed,
                sw.ElapsedMilliseconds, "Authentication failed (check token)");
        }
        catch (SyncTransportException ex)
        {
            _logger.Warn(ex, "Sync transport error");
            return new SyncResult(totalFetched, totalInserted, totalSkipped, totalFailed,
                sw.ElapsedMilliseconds, "Sync server unreachable");
        }
    }

    private async Task<(int Fetched, int Inserted, int Skipped, int Failed)> SyncNewsBriefsAsync(
        SyncConnectionInfo connection, DateOnly from, DateOnly to,
        IProgress<SyncProgressUpdate>? progress, CancellationToken ct)
    {
        progress?.Report(new SyncProgressUpdate("News Briefs", 0, 1, "Fetching..."));
        var dtos = await _client.FetchNewsBriefRunsAsync(connection, from, to, ct);
        int inserted = 0, skipped = 0, failed = 0;

        for (int idx = 0; idx < dtos.Count; idx++)
        {
            var dto = dtos[idx];
            try
            {
                var runId = Guid.Parse(dto.RunId);
                if (await _newsBriefRepo.ExistsAsync(runId, ct))
                {
                    skipped++;
                    continue;
                }

                var domain = _mapper.ToDomain(dto);
                await _newsBriefRepo.SaveAsync(domain, ct);
                inserted++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.Warn(ex, "Failed to sync news brief run {RunId}", dto.RunId);
            }

            progress?.Report(new SyncProgressUpdate("News Briefs", idx + 1, dtos.Count,
                $"{inserted} inserted, {skipped} skipped"));
        }

        _logger.Info("News briefs: {Fetched} fetched, {Inserted} inserted, {Skipped} skipped, {Failed} failed",
            dtos.Count, inserted, skipped, failed);
        return (dtos.Count, inserted, skipped, failed);
    }

    private async Task<(int Fetched, int Inserted, int Skipped, int Failed)> SyncWeeklySummariesAsync(
        SyncConnectionInfo connection, DateOnly from, DateOnly to,
        IProgress<SyncProgressUpdate>? progress, CancellationToken ct)
    {
        progress?.Report(new SyncProgressUpdate("Weekly Summaries", 0, 1, "Fetching..."));
        var dtos = await _client.FetchWeeklySummaryRunsAsync(connection, from, to, ct);
        int inserted = 0, skipped = 0, failed = 0;

        for (int idx = 0; idx < dtos.Count; idx++)
        {
            var dto = dtos[idx];
            try
            {
                var runId = Guid.Parse(dto.RunId);
                if (await _weeklySummaryRepo.ExistsAsync(runId, ct))
                {
                    skipped++;
                    continue;
                }

                var domain = _mapper.ToDomain(dto);
                await _weeklySummaryRepo.SaveAsync(domain, ct);
                inserted++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.Warn(ex, "Failed to sync weekly summary run {RunId}", dto.RunId);
            }

            progress?.Report(new SyncProgressUpdate("Weekly Summaries", idx + 1, dtos.Count,
                $"{inserted} inserted, {skipped} skipped"));
        }

        _logger.Info("Weekly summaries: {Fetched} fetched, {Inserted} inserted, {Skipped} skipped, {Failed} failed",
            dtos.Count, inserted, skipped, failed);
        return (dtos.Count, inserted, skipped, failed);
    }

    private async Task<(int Fetched, int Inserted, int Skipped, int Failed)> SyncSubstitutionChainsAsync(
        SyncConnectionInfo connection, DateOnly from, DateOnly to,
        IProgress<SyncProgressUpdate>? progress, CancellationToken ct)
    {
        progress?.Report(new SyncProgressUpdate("Substitution Chains", 0, 1, "Fetching..."));
        var dtos = await _client.FetchSubstitutionChainRunsAsync(connection, from, to, ct);
        int inserted = 0, skipped = 0, failed = 0;

        for (int idx = 0; idx < dtos.Count; idx++)
        {
            var dto = dtos[idx];
            try
            {
                var runId = Guid.Parse(dto.RunId);
                if (await _substitutionChainRepo.ExistsAsync(runId, ct))
                {
                    skipped++;
                    continue;
                }

                var domain = _mapper.ToDomain(dto);
                await _substitutionChainRepo.SaveAsync(domain, ct);
                inserted++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.Warn(ex, "Failed to sync substitution chain run {RunId}", dto.RunId);
            }

            progress?.Report(new SyncProgressUpdate("Substitution Chains", idx + 1, dtos.Count,
                $"{inserted} inserted, {skipped} skipped"));
        }

        _logger.Info("Substitution chains: {Fetched} fetched, {Inserted} inserted, {Skipped} skipped, {Failed} failed",
            dtos.Count, inserted, skipped, failed);
        return (dtos.Count, inserted, skipped, failed);
    }

    private async Task<(int Fetched, int Inserted, int Skipped, int Failed)> SyncOpportunityScansAsync(
        SyncConnectionInfo connection, DateOnly from, DateOnly to,
        IProgress<SyncProgressUpdate>? progress, CancellationToken ct)
    {
        progress?.Report(new SyncProgressUpdate("Opportunity Scans", 0, 1, "Fetching..."));
        var dtos = await _client.FetchOpportunityScanRunsAsync(connection, from, to, ct);
        int inserted = 0, skipped = 0, failed = 0;

        for (int idx = 0; idx < dtos.Count; idx++)
        {
            var dto = dtos[idx];
            try
            {
                var runId = Guid.Parse(dto.RunId);
                if (await _opportunityScanRepo.ExistsAsync(runId, ct))
                {
                    skipped++;
                    continue;
                }

                var domain = _mapper.ToDomain(dto);
                await _opportunityScanRepo.SaveAsync(domain, ct);
                inserted++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.Warn(ex, "Failed to sync opportunity scan run {RunId}", dto.RunId);
            }

            progress?.Report(new SyncProgressUpdate("Opportunity Scans", idx + 1, dtos.Count,
                $"{inserted} inserted, {skipped} skipped"));
        }

        _logger.Info("Opportunity scans: {Fetched} fetched, {Inserted} inserted, {Skipped} skipped, {Failed} failed",
            dtos.Count, inserted, skipped, failed);
        return (dtos.Count, inserted, skipped, failed);
    }
}

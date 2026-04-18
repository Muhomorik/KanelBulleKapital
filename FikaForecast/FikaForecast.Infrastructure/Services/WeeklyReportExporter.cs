using System.Globalization;
using System.IO;
using System.Text;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;
using NLog;

namespace FikaForecast.Infrastructure.Services;

/// <summary>
/// Writes weekly report markdown files under the user-configured export folder
/// using <see cref="ReportExportMarkdownFormatter"/>. Auto-export methods are
/// no-ops when <see cref="IExportSettingsProvider.AutoExportEnabled"/> is off;
/// all exceptions are swallowed and logged so export failures never break a pipeline run.
/// </summary>
public class WeeklyReportExporter : IWeeklyReportExporter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly ILogger _logger;
    private readonly IExportSettingsProvider _settings;
    private readonly ReportExportMarkdownFormatter _formatter;
    private readonly IWeeklySummaryRunRepository _weeklyRepo;
    private readonly ISubstitutionChainRunRepository _chainRepo;
    private readonly IOpportunityScanRunRepository _scanRepo;

    public WeeklyReportExporter(
        ILogger logger,
        IExportSettingsProvider settings,
        ReportExportMarkdownFormatter formatter,
        IWeeklySummaryRunRepository weeklyRepo,
        ISubstitutionChainRunRepository chainRepo,
        IOpportunityScanRunRepository scanRepo)
    {
        _logger = logger;
        _settings = settings;
        _formatter = formatter;
        _weeklyRepo = weeklyRepo;
        _chainRepo = chainRepo;
        _scanRepo = scanRepo;
    }

    public async Task ExportWeeklySummaryAsync(WeeklySummaryRun run, CancellationToken cancellationToken = default)
    {
        if (!TryGetAutoExportFolder(out var folder)) return;
        if (!IsExportable(run.Status, nameof(WeeklySummaryRun), run.RunId)) return;

        try
        {
            var ctx = BuildContext(ReportType.WeeklySummary, run.WeekStart, run.WeekEnd, run.Timestamp, run.ModelId, run.RunId, run.RawMarkdownOutput);
            await WriteAsync(folder, ctx, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Auto-export failed for WeeklySummaryRun {0}", run.RunId);
        }
    }

    public async Task ExportSubstitutionChainAsync(SubstitutionChainRun run, CancellationToken cancellationToken = default)
    {
        if (!TryGetAutoExportFolder(out var folder)) return;
        if (!IsExportable(run.Status, nameof(SubstitutionChainRun), run.RunId)) return;

        try
        {
            var weekly = await _weeklyRepo.GetByIdAsync(run.WeeklySummaryRunId, cancellationToken);
            if (weekly is null)
            {
                _logger.Warn("Cannot export SubstitutionChainRun {0}: parent WeeklySummaryRun {1} not found", run.RunId, run.WeeklySummaryRunId);
                return;
            }

            var ctx = BuildContext(ReportType.SubstitutionChain, weekly.WeekStart, weekly.WeekEnd, run.Timestamp, run.ModelId, run.RunId, run.RawMarkdownOutput);
            await WriteAsync(folder, ctx, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Auto-export failed for SubstitutionChainRun {0}", run.RunId);
        }
    }

    public async Task ExportOpportunityScanAsync(OpportunityScanRun run, CancellationToken cancellationToken = default)
    {
        if (!TryGetAutoExportFolder(out var folder)) return;
        if (!IsExportable(run.Status, nameof(OpportunityScanRun), run.RunId)) return;

        try
        {
            var chain = await _chainRepo.GetByIdAsync(run.SubstitutionChainRunId, cancellationToken);
            if (chain is null)
            {
                _logger.Warn("Cannot export OpportunityScanRun {0}: parent SubstitutionChainRun {1} not found", run.RunId, run.SubstitutionChainRunId);
                return;
            }
            var weekly = await _weeklyRepo.GetByIdAsync(chain.WeeklySummaryRunId, cancellationToken);
            if (weekly is null)
            {
                _logger.Warn("Cannot export OpportunityScanRun {0}: grandparent WeeklySummaryRun {1} not found", run.RunId, chain.WeeklySummaryRunId);
                return;
            }

            var ctx = BuildContext(ReportType.RotationTargets, weekly.WeekStart, weekly.WeekEnd, run.Timestamp, run.ModelId, run.RunId, run.RawMarkdownOutput);
            await WriteAsync(folder, ctx, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Auto-export failed for OpportunityScanRun {0}", run.RunId);
        }
    }

    public async Task<IReadOnlyList<string>> ExportAllForWeekAsync(DateTimeOffset weekStart, CancellationToken cancellationToken = default)
    {
        var folder = _settings.ExportFolderPath;
        if (string.IsNullOrWhiteSpace(folder))
            throw new InvalidOperationException("Export folder is not configured.");

        var (year, week) = IsoWeek.Compute(weekStart);
        var written = new List<string>();

        var allWeekly = await _weeklyRepo.GetAllAsync(cancellationToken);
        var weekly = allWeekly
            .Where(r => r.Status == RunStatus.Success && MatchesIsoWeek(r.WeekStart, year, week))
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefault();

        if (weekly is null)
        {
            _logger.Warn("No successful weekly summary run for ISO week {0}-W{1:00}; nothing to export", year, week);
            return written;
        }

        written.Add(await WriteAsync(folder,
            BuildContext(ReportType.WeeklySummary, weekly.WeekStart, weekly.WeekEnd, weekly.Timestamp, weekly.ModelId, weekly.RunId, weekly.RawMarkdownOutput),
            cancellationToken));

        var allChains = await _chainRepo.GetAllAsync(cancellationToken);
        var chain = allChains
            .Where(r => r.Status == RunStatus.Success && r.WeeklySummaryRunId == weekly.RunId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefault();

        if (chain is not null)
        {
            written.Add(await WriteAsync(folder,
                BuildContext(ReportType.SubstitutionChain, weekly.WeekStart, weekly.WeekEnd, chain.Timestamp, chain.ModelId, chain.RunId, chain.RawMarkdownOutput),
                cancellationToken));

            var allScans = await _scanRepo.GetAllAsync(cancellationToken);
            var scan = allScans
                .Where(r => r.Status == RunStatus.Success && r.SubstitutionChainRunId == chain.RunId)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefault();

            if (scan is not null)
            {
                written.Add(await WriteAsync(folder,
                    BuildContext(ReportType.RotationTargets, weekly.WeekStart, weekly.WeekEnd, scan.Timestamp, scan.ModelId, scan.RunId, scan.RawMarkdownOutput),
                    cancellationToken));
            }
        }

        _logger.Info("Manual export for {0}-W{1:00}: wrote {2} file(s) to {3}", year, week, written.Count, folder);
        return written;
    }

    private bool TryGetAutoExportFolder(out string folder)
    {
        folder = string.Empty;
        if (!_settings.AutoExportEnabled)
        {
            _logger.Debug("Auto-export skipped: disabled in settings");
            return false;
        }
        var path = _settings.ExportFolderPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.Debug("Auto-export skipped: export folder is not configured");
            return false;
        }
        folder = path;
        return true;
    }

    private bool IsExportable(RunStatus status, string kind, Guid runId)
    {
        if (status == RunStatus.Success) return true;
        _logger.Warn("Skipping export for {0} {1}: status is {2}", kind, runId, status);
        return false;
    }

    private static ExportReportContext BuildContext(
        ReportType type,
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd,
        DateTimeOffset generatedAt,
        string modelId,
        Guid runId,
        string body) =>
        new(type, weekStart, weekEnd, generatedAt, modelId, runId, body ?? string.Empty);

    private async Task<string> WriteAsync(string folder, ExportReportContext ctx, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folder);
        var fileName = BuildFileName(ctx);
        var fullPath = Path.Combine(folder, fileName);
        var content = _formatter.Format(ctx);
        await File.WriteAllTextAsync(fullPath, content, Utf8NoBom, cancellationToken);
        _logger.Info("Exported {0} for {1} to {2}", ctx.Type, ctx.RunId, fullPath);
        return fullPath;
    }

    private static string BuildFileName(ExportReportContext ctx)
    {
        var (year, week) = IsoWeek.Compute(ctx.WeekStart);
        var slug = ReportExportMarkdownFormatter.FileSlug(ctx.Type);
        return string.Create(CultureInfo.InvariantCulture, $"{year:0000}-W{week:00}-{slug}.md");
    }

    private static bool MatchesIsoWeek(DateTimeOffset instant, int year, int week)
    {
        var (y, w) = IsoWeek.Compute(instant);
        return y == year && w == week;
    }
}

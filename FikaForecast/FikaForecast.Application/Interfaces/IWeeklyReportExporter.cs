using FikaForecast.Domain.Entities;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Writes weekly report markdown files to the configured export folder. Auto-export
/// methods no-op when <see cref="IExportSettingsProvider.AutoExportEnabled"/> is false
/// or the folder path is blank. Exceptions are swallowed and logged — a failed export
/// must never break the pipeline run that triggered it.
/// </summary>
public interface IWeeklyReportExporter
{
    /// <summary>Auto-export the weekly summary after its run completes. No-ops when disabled.</summary>
    Task ExportWeeklySummaryAsync(WeeklySummaryRun run, CancellationToken cancellationToken = default);

    /// <summary>Auto-export the substitution chain. Resolves parent week via FK. No-ops when disabled.</summary>
    Task ExportSubstitutionChainAsync(SubstitutionChainRun run, CancellationToken cancellationToken = default);

    /// <summary>Auto-export the opportunity scan / rotation targets. Resolves parent week via FK chain. No-ops when disabled.</summary>
    Task ExportOpportunityScanAsync(OpportunityScanRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manual export: writes every available report type for the given week
    /// regardless of the auto-export toggle. Throws <see cref="InvalidOperationException"/>
    /// if the export folder is not configured.
    /// </summary>
    /// <returns>Paths of files that were written.</returns>
    Task<IReadOnlyList<string>> ExportAllForWeekAsync(DateTimeOffset weekStart, CancellationToken cancellationToken = default);
}

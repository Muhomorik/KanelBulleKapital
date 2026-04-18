namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Provides the current export configuration to the exporter without coupling it
/// to the Wpf-layer settings store. Implemented in the presentation layer on top
/// of <c>IUserSettingsService</c>.
/// </summary>
public interface IExportSettingsProvider
{
    /// <summary>Absolute folder where exported markdown files are written, or <c>null</c>/empty if not set.</summary>
    string? ExportFolderPath { get; }

    /// <summary>When false, auto-export hooks in the orchestrators should no-op.</summary>
    bool AutoExportEnabled { get; }
}

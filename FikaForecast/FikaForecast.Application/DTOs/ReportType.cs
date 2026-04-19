namespace FikaForecast.Application.DTOs;

/// <summary>
/// Identifies which weekly pipeline report a <see cref="ExportReportContext"/> represents.
/// Used to pick the H1 label and filename slug for exported markdown.
/// </summary>
public enum ReportType
{
    WeeklySummary,
    SubstitutionChain,
    RotationTargets
}

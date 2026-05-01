namespace FikaForecast.Application.DTOs;

/// <summary>
/// All data needed to render a single weekly report as an export markdown file.
/// The <paramref name="Body"/> is the already-rendered display markdown
/// (<c>RawMarkdownOutput</c> from the run entity) and is embedded verbatim between export fences.
/// </summary>
public record ExportReportContext(
    ReportType Type,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    DateTimeOffset GeneratedAt,
    string ModelId,
    Guid RunId,
    string Body);

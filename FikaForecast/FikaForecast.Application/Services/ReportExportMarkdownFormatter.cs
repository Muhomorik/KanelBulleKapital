using System.Globalization;
using System.Text;
using FikaForecast.Application.DTOs;

namespace FikaForecast.Application.Services;

/// <summary>
/// Wraps a report's existing display markdown with a standardized H1, a YAML metadata
/// block fenced by <c>BEGIN/END METADATA</c>, and the report body fenced by
/// <c>BEGIN/END REPORT</c>. Output is consumed by an external fund analytics AI agent
/// that may concatenate multiple reports; the paired fences give deterministic split
/// points and the YAML block gives a stable, machine-parseable metadata schema.
/// </summary>
public class ReportExportMarkdownFormatter
{
    /// <summary>
    /// Produces the export-ready markdown for a single report. Uses invariant culture
    /// for dates — exported files are machine-consumed and should not vary by host locale.
    /// </summary>
    public string Format(ExportReportContext ctx)
    {
        var label = TypeLabel(ctx.Type);
        var slug = FileSlug(ctx.Type);
        var (year, week) = IsoWeek.Compute(ctx.PeriodStart);
        var weekTag = string.Create(CultureInfo.InvariantCulture, $"{year:0000}-W{week:00}");
        var range = FormatRange(ctx.PeriodStart, ctx.PeriodEnd);

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"# {label} — Week {week} ({range})");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("<!-- BEGIN METADATA -->");
        sb.AppendLine("```yaml");
        sb.Append(CultureInfo.InvariantCulture, $"report_type: {slug}");
        sb.AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"iso_week: {weekTag}");
        sb.AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"period_start: {ctx.PeriodStart:yyyy-MM-dd}");
        sb.AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"period_end: {ctx.PeriodEnd:yyyy-MM-dd}");
        sb.AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"generated_at: {ctx.GeneratedAt:yyyy-MM-ddTHH:mm:sszzz}");
        sb.AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"model: {ctx.ModelId}");
        sb.AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"run_id: {ctx.RunId}");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("<!-- END METADATA -->");
        sb.AppendLine();
        sb.AppendLine("<!-- BEGIN REPORT -->");
        sb.AppendLine();
        sb.AppendLine(ctx.Body.TrimEnd());
        sb.AppendLine();
        sb.AppendLine("<!-- END REPORT -->");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Filename slug used by the exporter. Kept here so the formatter and exporter
    /// can't drift on naming.
    /// </summary>
    public static string FileSlug(ReportType type) => type switch
    {
        ReportType.WeeklySummary => "weekly-summary",
        ReportType.SubstitutionChain => "substitution-chain",
        ReportType.RotationTargets => "rotation-targets",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown report type")
    };

    /// <summary>
    /// Human-readable name shown in the H1 and metadata block.
    /// </summary>
    public static string TypeLabel(ReportType type) => type switch
    {
        ReportType.WeeklySummary => "Weekly Summary",
        ReportType.SubstitutionChain => "Substitution Chain",
        ReportType.RotationTargets => "Rotation Targets",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown report type")
    };

    private static string FormatRange(DateTimeOffset start, DateTimeOffset end)
    {
        var ci = CultureInfo.InvariantCulture;
        if (start.Year == end.Year)
        {
            return string.Create(ci, $"{start:MMM d} – {end:MMM d, yyyy}");
        }
        return string.Create(ci, $"{start:MMM d, yyyy} – {end:MMM d, yyyy}");
    }
}

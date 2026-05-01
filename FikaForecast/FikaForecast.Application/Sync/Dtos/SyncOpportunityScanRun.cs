namespace FikaForecast.Application.Sync.Dtos;

/// <summary>
/// Wire DTO mirroring backend <c>KanelBrief.Core.Models.OpportunityScanRun</c>.
/// </summary>
public sealed class SyncOpportunityScanRun
{
    // AgentRunBase fields
    public string RunDate { get; set; } = "";
    public string RunId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string ModelId { get; set; } = "";
    public string Status { get; set; } = "";
    public double DurationSeconds { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }

    // OpportunityScanRun-specific
    public string ReportType { get; set; } = "rotation-targets";
    public string SubstitutionChainRunId { get; set; } = "";
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public string PeriodIsoWeek { get; set; } = "";
    public List<SyncRotationTarget> Targets { get; set; } = [];
}

public sealed class SyncRotationTarget
{
    public string Category { get; set; } = "";
    public string SignalStrength { get; set; } = "";
    public string Rationale { get; set; } = "";
    public string RiskCaveat { get; set; } = "";
}

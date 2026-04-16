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
    public int Status { get; set; }
    public double DurationSeconds { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }

    // OpportunityScanRun-specific
    public string SubstitutionChainRunId { get; set; } = "";
    public List<SyncRotationTarget> Targets { get; set; } = [];
}

public sealed class SyncRotationTarget
{
    public string Category { get; set; } = "";
    public int SignalStrength { get; set; }
    public string Rationale { get; set; } = "";
    public string RiskCaveat { get; set; } = "";
}

namespace FikaForecast.Application.Sync.Dtos;

/// <summary>
/// Wire DTO mirroring backend <c>KanelBrief.Core.Models.SubstitutionChainRun</c>.
/// </summary>
public sealed class SyncSubstitutionChainRun
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

    // SubstitutionChainRun-specific
    public string ReportType { get; set; } = "substitution-chain";
    public string WeeklySummaryRunId { get; set; } = "";
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public string PeriodIsoWeek { get; set; } = "";
    public List<SyncRotationChain> Chains { get; set; } = [];
}

public sealed class SyncRotationChain
{
    public string CapitalFleeing { get; set; } = "";
    public string FlowsToward { get; set; } = "";
    public string Mechanism { get; set; } = "";
}

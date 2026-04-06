namespace KanelBrief.Core.Models;

/// <summary>Shared properties for all agent run results. Maps to Azure Tables PartitionKey (RunDate) and RowKey (RunId).</summary>
public abstract class AgentRunBase
{
    /// <summary>Date of the run, e.g. "2026-04-05". Maps to Azure Tables PartitionKey.</summary>
    public string RunDate { get; set; } = string.Empty;

    /// <summary>Unique run identifier (GUID). Maps to Azure Tables RowKey.</summary>
    public string RunId { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public RunStatus Status { get; set; }

    public double DurationSeconds { get; set; }

    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int TotalTokens { get; set; }
}

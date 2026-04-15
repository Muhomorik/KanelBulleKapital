namespace KanelBrief.Core.Models;

/// <summary>
/// Opportunity Scan agent output plus token usage captured from the LLM response.
/// Targets are populated by JSON deserialization; token counts
/// are set by the analyzer after the call returns.
/// </summary>
public class OpportunityScanAnalysisResult
{
    public List<RotationTarget> Targets { get; set; } = [];
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}

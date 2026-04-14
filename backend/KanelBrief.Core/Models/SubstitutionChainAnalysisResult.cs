namespace KanelBrief.Core.Models;

/// <summary>
/// Substitution Chain agent output plus token usage captured from the LLM response.
/// Chains are populated by JSON deserialization; token counts
/// are set by the analyzer after the call returns.
/// </summary>
public class SubstitutionChainAnalysisResult
{
    public List<RotationChain> Chains { get; set; } = [];
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}

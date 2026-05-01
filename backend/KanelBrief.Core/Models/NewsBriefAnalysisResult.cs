namespace KanelBrief.Core.Models;

/// <summary>
/// News Brief agent output plus token usage captured from the LLM response.
/// Mood/Summary/Assessments are populated by JSON deserialization; token counts
/// are set by the analyzer after the call returns.
/// </summary>
public class NewsBriefAnalysisResult
{
    public string Mood { get; set; } = MarketSentiment.Mixed.ToString();
    public string Summary { get; set; } = string.Empty;
    public List<CategoryAssessment> Assessments { get; set; } = [];
    public List<Citation> Citations { get; set; } = [];
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}

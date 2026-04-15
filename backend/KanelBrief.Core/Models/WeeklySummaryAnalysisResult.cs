namespace KanelBrief.Core.Models;

/// <summary>
/// Weekly Summary agent output plus token usage captured from the LLM response.
/// Mood/Summary/Themes are populated by JSON deserialization; token counts
/// are set by the analyzer after the call returns.
/// </summary>
public class WeeklySummaryAnalysisResult
{
    public string Mood { get; set; } = MarketSentiment.Mixed.ToString();
    public string Summary { get; set; } = string.Empty;
    public List<WeeklySummaryTheme> Themes { get; set; } = [];
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}

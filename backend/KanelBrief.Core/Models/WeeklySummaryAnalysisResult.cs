namespace KanelBrief.Core.Models;

/// <summary>Deserialization model for Weekly Summary agent JSON response.</summary>
public class WeeklySummaryAnalysisResult
{
    public string Mood { get; set; } = MarketSentiment.Mixed.ToString();
    public string Summary { get; set; } = string.Empty;
    public List<WeeklySummaryTheme> Themes { get; set; } = [];
}

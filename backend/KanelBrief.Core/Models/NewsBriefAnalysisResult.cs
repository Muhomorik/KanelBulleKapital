namespace KanelBrief.Core.Models;

/// <summary>Deserialization model for News Brief agent JSON response.</summary>
public class NewsBriefAnalysisResult
{
    public string Mood { get; set; } = MarketSentiment.Mixed.ToString();
    public string Summary { get; set; } = string.Empty;
    public List<CategoryAssessment> Assessments { get; set; } = [];
}

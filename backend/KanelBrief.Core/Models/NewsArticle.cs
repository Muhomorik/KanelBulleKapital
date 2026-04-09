namespace KanelBrief.Core.Models;

/// <summary>Input model for news articles fed to the News Brief agent.</summary>
public class NewsArticle
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

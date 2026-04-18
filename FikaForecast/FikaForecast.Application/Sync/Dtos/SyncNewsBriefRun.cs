namespace FikaForecast.Application.Sync.Dtos;

/// <summary>
/// Wire DTO mirroring backend <c>KanelBrief.Core.Models.NewsBriefRun</c>.
/// Flat assessments — no <c>NewsItem</c> wrapper, no <c>PromptName</c>,
/// no <c>RawAgentOutput</c>, no <c>RawMarkdownOutput</c>.
/// </summary>
public sealed class SyncNewsBriefRun
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

    // NewsBriefRun-specific
    public string DeploymentName { get; set; } = "";
    public string Mood { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<SyncCategoryAssessment> Assessments { get; set; } = [];
}

public sealed class SyncCategoryAssessment
{
    public string Category { get; set; } = "";
    public string Headline { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Sentiment { get; set; } = "";
}

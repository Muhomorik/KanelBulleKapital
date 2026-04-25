namespace KanelBrief.Core.Models;

/// <summary>Result of a single News Brief agent run. Stored in the <c>NewsBriefRuns</c> Azure Table.</summary>
public class NewsBriefRun : AgentRunBase
{
    public string DeploymentName { get; set; } = string.Empty;

    public string Mood { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public List<CategoryAssessment> Assessments { get; set; } = [];

    public List<Citation> Citations { get; set; } = [];
}

namespace KanelBrief.Core.Models;

/// <summary>Deserialization model for Opportunity Scan agent JSON response.</summary>
public class OpportunityScanAnalysisResult
{
    public List<RotationTarget> Targets { get; set; } = [];
}

namespace KanelBrief.Core.Models;

/// <summary>Deserialization model for Substitution Chain agent JSON response.</summary>
public class SubstitutionChainAnalysisResult
{
    public List<RotationChain> Chains { get; set; } = [];
}

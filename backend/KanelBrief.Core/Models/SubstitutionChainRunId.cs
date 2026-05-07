namespace KanelBrief.Core.Models;

/// <summary>Strongly-typed identifier for a <see cref="SubstitutionChainRun"/>.</summary>
public readonly record struct SubstitutionChainRunId(string Value)
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
    public static SubstitutionChainRunId NewId() => new(Guid.NewGuid().ToString());
    public static implicit operator string(SubstitutionChainRunId id) => id.Value ?? string.Empty;
    public static implicit operator SubstitutionChainRunId(string value) => new(value);
    public override string ToString() => Value ?? string.Empty;
}

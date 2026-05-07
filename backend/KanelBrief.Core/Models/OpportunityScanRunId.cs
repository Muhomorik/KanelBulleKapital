namespace KanelBrief.Core.Models;

/// <summary>Strongly-typed identifier for an <see cref="OpportunityScanRun"/>.</summary>
public readonly record struct OpportunityScanRunId(string Value)
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
    public static OpportunityScanRunId NewId() => new(Guid.NewGuid().ToString());
    public static implicit operator string(OpportunityScanRunId id) => id.Value ?? string.Empty;
    public static implicit operator OpportunityScanRunId(string value) => new(value);
    public override string ToString() => Value ?? string.Empty;
}

namespace KanelBrief.Core.Models;

/// <summary>Strongly-typed identifier for a <see cref="NewsBriefRun"/>.</summary>
public readonly record struct NewsBriefRunId(string Value)
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
    public static NewsBriefRunId NewId() => new(Guid.NewGuid().ToString());
    public static implicit operator string(NewsBriefRunId id) => id.Value ?? string.Empty;
    public static implicit operator NewsBriefRunId(string value) => new(value);
    public override string ToString() => Value ?? string.Empty;
}

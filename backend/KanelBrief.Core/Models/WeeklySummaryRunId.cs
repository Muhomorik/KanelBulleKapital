namespace KanelBrief.Core.Models;

/// <summary>Strongly-typed identifier for a <see cref="WeeklySummaryRun"/>.</summary>
public readonly record struct WeeklySummaryRunId(string Value)
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
    public static WeeklySummaryRunId NewId() => new(Guid.NewGuid().ToString());
    public static implicit operator string(WeeklySummaryRunId id) => id.Value ?? string.Empty;
    public static implicit operator WeeklySummaryRunId(string value) => new(value);
    public override string ToString() => Value ?? string.Empty;
}

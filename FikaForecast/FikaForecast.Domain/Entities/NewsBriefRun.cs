using System.Diagnostics;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// Aggregate root representing a single execution of the News Brief Agent.
/// Tracks the model used, token consumption, duration, and parsed output.
/// </summary>
[DebuggerDisplay("{DeploymentName} — {Status} ({TotalTokens} tokens, {Duration.TotalSeconds:F1}s)")]
public class NewsBriefRun
{
    private readonly List<NewsItem> _items = [];

    public Guid RunId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string ModelId { get; private set; }
    public string DeploymentName { get; private set; }
    public string PromptName { get; private set; }
    public TimeSpan Duration { get; private set; }
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int TotalTokens { get; private set; }
    public RunStatus Status { get; private set; }
    public string RawMarkdownOutput { get; private set; }
    public MarketMood? Mood { get; private set; }

    /// <summary>Parsed news items from the agent's markdown output.</summary>
    public IReadOnlyList<NewsItem> Items => _items.AsReadOnly();

    private NewsBriefRun() // EF Core
    {
        ModelId = null!;
        DeploymentName = null!;
        PromptName = null!;
        RawMarkdownOutput = null!;
    }

    /// <summary>
    /// Creates a new run in <see cref="RunStatus.Partial"/> state.
    /// Call <see cref="Complete"/> or <see cref="Fail"/> when the agent finishes.
    /// </summary>
    public static NewsBriefRun Start(ModelConfig model, AgentPrompt prompt)
    {
        return new NewsBriefRun
        {
            RunId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.Now,
            ModelId = model.ModelId,
            DeploymentName = model.DeploymentName,
            PromptName = prompt.Name,
            Status = RunStatus.Partial,
            RawMarkdownOutput = string.Empty
        };
    }

    /// <summary>
    /// Marks the run as successful and records output and token usage.
    /// </summary>
    public void Complete(
        string rawMarkdownOutput,
        TimeSpan duration,
        int inputTokens,
        int outputTokens)
    {
        RawMarkdownOutput = rawMarkdownOutput;
        Duration = duration;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        TotalTokens = inputTokens + outputTokens;
        Status = RunStatus.Success;
    }

    /// <summary>
    /// Marks the run as failed.
    /// </summary>
    public void Fail(TimeSpan duration)
    {
        Duration = duration;
        Status = RunStatus.Failed;
    }

    /// <summary>
    /// Sets the overall market mood parsed from the brief's closing summary.
    /// </summary>
    public void SetMood(MarketMood mood)
    {
        Mood = mood;
    }

    /// <summary>
    /// Adds a parsed news item to this run.
    /// </summary>
    public void AddItem(NewsItem item)
    {
        _items.Add(item);
    }
}

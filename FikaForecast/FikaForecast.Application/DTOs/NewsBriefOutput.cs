namespace FikaForecast.Application.DTOs;

/// <summary>
/// JSON deserialization target for the News Brief Agent's structured output.
/// </summary>
public sealed record NewsBriefOutput(
    string Mood,
    string Summary,
    List<NewsBriefOutputCategory> Categories);

/// <summary>
/// A single per-category news item from the agent's JSON output.
/// </summary>
public sealed record NewsBriefOutputCategory(
    string Category,
    string Headline,
    string Summary,
    string Sentiment);

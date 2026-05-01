using System.Text.Json;

using KanelBrief.Core.Models;
using KanelBrief.Core.Serialization;

namespace KanelBrief.Functions.Tests.Api;

/// <summary>
/// Freezes the <c>/api/sync/*</c> wire contract so the FikaForecast desktop client never has to
/// guess what comes off the wire. Each test serializes a fully-populated domain model with
/// <see cref="KanelJsonOptions.CamelCase"/> (the same options the HTTP endpoint uses) and asserts
/// the resulting JSON against the client's deserialization assumptions:
/// camelCase property names, enums-as-strings (<c>"Success"</c>, <c>"RiskOn"</c>), no PascalCase leak.
/// If any of these break, the WPF sync client will crash on the first sync.
/// </summary>
[TestFixture]
[TestOf(typeof(SyncRunsResponse<>))]
public class SyncApiContractTests
{
    #region Envelope shape

    [Test]
    public void Envelope_Serialized_UsesCamelCaseFields()
    {
        // Arrange
        var payload = new SyncRunsResponse<NewsBriefRun>
        {
            From = "2026-04-15",
            To = "2026-04-16",
            Count = 0,
            Runs = []
        };

        // Act
        var json = JsonSerializer.Serialize(payload, KanelJsonOptions.CamelCase);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.That(root.TryGetProperty("from", out _), Is.True);
        Assert.That(root.TryGetProperty("to", out _), Is.True);
        Assert.That(root.TryGetProperty("count", out _), Is.True);
        Assert.That(root.TryGetProperty("runs", out _), Is.True);
        Assert.That(root.TryGetProperty("From", out _), Is.False, "No PascalCase leak");
        Assert.That(root.TryGetProperty("Runs", out _), Is.False, "No PascalCase leak");
    }

    #endregion

    #region Per-run-type wire format

    [Test]
    public void NewsBriefRun_Serialized_EmitsStatusAsStringAndSentimentAsString()
    {
        // Arrange
        var payload = new SyncRunsResponse<NewsBriefRun>
        {
            From = "2026-04-15",
            To = "2026-04-16",
            Count = 1,
            Runs =
            [
                new NewsBriefRun
                {
                    RunDate = "2026-04-16",
                    RunId = "run-abc",
                    CreatedAt = DateTimeOffset.Parse("2026-04-16T12:00:00Z"),
                    ModelId = "gpt-5.4-mini",
                    Status = RunStatus.Success,
                    DurationSeconds = 1.5,
                    InputTokens = 100,
                    OutputTokens = 200,
                    TotalTokens = 300,
                    DeploymentName = "gpt-5.4-mini",
                    Mood = "RiskOn",
                    Summary = "Markets up",
                    Assessments =
                    [
                        new CategoryAssessment
                        {
                            Category = "Tech",
                            Headline = "H",
                            Summary = "S",
                            Sentiment = MarketSentiment.RiskOn
                        }
                    ]
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(payload, KanelJsonOptions.CamelCase);
        using var doc = JsonDocument.Parse(json);
        var run = doc.RootElement.GetProperty("runs")[0];
        var assessment = run.GetProperty("assessments")[0];

        // Assert
        Assert.That(run.GetProperty("status").ValueKind, Is.EqualTo(JsonValueKind.String),
            "Status must be a JSON string, not an integer — WPF DTO expects string.");
        Assert.That(run.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(assessment.GetProperty("sentiment").ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(assessment.GetProperty("sentiment").GetString(), Is.EqualTo("RiskOn"));
        Assert.That(run.TryGetProperty("runId", out _), Is.True);
        Assert.That(run.TryGetProperty("createdAt", out _), Is.True);
        Assert.That(run.TryGetProperty("durationSeconds", out _), Is.True);
        Assert.That(run.TryGetProperty("RunId", out _), Is.False);
    }

    [Test]
    public void WeeklySummaryRun_Serialized_EmitsAllEnumsAsStrings()
    {
        // Arrange
        var payload = new SyncRunsResponse<WeeklySummaryRun>
        {
            From = "2026-04-15",
            To = "2026-04-16",
            Count = 1,
            Runs =
            [
                new WeeklySummaryRun
                {
                    RunDate = "2026-04-16",
                    RunId = "weekly-001",
                    CreatedAt = DateTimeOffset.Parse("2026-04-16T12:00:00Z"),
                    ModelId = "gpt-5.4-mini",
                    Status = RunStatus.Success,
                    DurationSeconds = 5.0,
                    InputTokens = 400,
                    OutputTokens = 500,
                    TotalTokens = 900,
                    PeriodStart = DateTimeOffset.Parse("2026-04-10T00:00:00Z"),
                    PeriodEnd = DateTimeOffset.Parse("2026-04-16T23:59:59Z"),
                    PeriodIsoWeek = "2026-W15",
                    NetMood = MarketSentiment.RiskOff,
                    MoodSummary = "Risk-off week",
                    Themes =
                    [
                        new WeeklySummaryTheme
                        {
                            Category = "Rates",
                            Summary = "Rising",
                            Confidence = ConfidenceLevel.Medium,
                            Sentiment = MarketSentiment.RiskOff
                        }
                    ]
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(payload, KanelJsonOptions.CamelCase);
        using var doc = JsonDocument.Parse(json);
        var run = doc.RootElement.GetProperty("runs")[0];
        var theme = run.GetProperty("themes")[0];

        // Assert
        Assert.That(run.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(run.GetProperty("netMood").GetString(), Is.EqualTo("RiskOff"));
        Assert.That(theme.GetProperty("confidence").GetString(), Is.EqualTo("Medium"),
            "Backend 'Medium' is mapped client-side to WPF 'Moderate'.");
        Assert.That(theme.GetProperty("sentiment").GetString(), Is.EqualTo("RiskOff"));
    }

    [Test]
    public void SubstitutionChainRun_Serialized_EmitsStatusAsString()
    {
        // Arrange
        var payload = new SyncRunsResponse<SubstitutionChainRun>
        {
            From = "2026-04-15",
            To = "2026-04-16",
            Count = 1,
            Runs =
            [
                new SubstitutionChainRun
                {
                    RunDate = "2026-04-16",
                    RunId = "chain-001",
                    CreatedAt = DateTimeOffset.Parse("2026-04-16T12:00:00Z"),
                    ModelId = "gpt-5.4-mini",
                    Status = RunStatus.Success,
                    DurationSeconds = 2.0,
                    InputTokens = 10,
                    OutputTokens = 20,
                    TotalTokens = 30,
                    WeeklySummaryRunId = "weekly-001",
                    Chains =
                    [
                        new RotationChain
                        {
                            CapitalFleeing = "Tech",
                            FlowsToward = "Energy",
                            Mechanism = "Rates"
                        }
                    ]
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(payload, KanelJsonOptions.CamelCase);
        using var doc = JsonDocument.Parse(json);
        var run = doc.RootElement.GetProperty("runs")[0];

        // Assert
        Assert.That(run.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(run.TryGetProperty("weeklySummaryRunId", out _), Is.True);
        Assert.That(run.GetProperty("chains")[0].TryGetProperty("capitalFleeing", out _), Is.True);
    }

    [Test]
    public void OpportunityScanRun_Serialized_EmitsStatusAndSignalStrengthAsStrings()
    {
        // Arrange
        var payload = new SyncRunsResponse<OpportunityScanRun>
        {
            From = "2026-04-15",
            To = "2026-04-16",
            Count = 1,
            Runs =
            [
                new OpportunityScanRun
                {
                    RunDate = "2026-04-16",
                    RunId = "opp-001",
                    CreatedAt = DateTimeOffset.Parse("2026-04-16T12:00:00Z"),
                    ModelId = "gpt-5.4-mini",
                    Status = RunStatus.Success,
                    DurationSeconds = 3.0,
                    InputTokens = 1,
                    OutputTokens = 2,
                    TotalTokens = 3,
                    SubstitutionChainRunId = "chain-001",
                    Targets =
                    [
                        new RotationTarget
                        {
                            Category = "Energy",
                            SignalStrength = SignalStrength.Strong,
                            Rationale = "Rotation intact",
                            RiskCaveat = "OPEC risk"
                        }
                    ]
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(payload, KanelJsonOptions.CamelCase);
        using var doc = JsonDocument.Parse(json);
        var run = doc.RootElement.GetProperty("runs")[0];
        var target = run.GetProperty("targets")[0];

        // Assert
        Assert.That(run.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(target.GetProperty("signalStrength").ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(target.GetProperty("signalStrength").GetString(), Is.EqualTo("Strong"));
    }

    #endregion

    #region Enum regression

    [TestCase(RunStatus.Success, "\"Success\"")]
    [TestCase(RunStatus.Failed, "\"Failed\"")]
    [TestCase(MarketSentiment.RiskOn, "\"RiskOn\"")]
    [TestCase(MarketSentiment.RiskOff, "\"RiskOff\"")]
    [TestCase(MarketSentiment.Mixed, "\"Mixed\"")]
    [TestCase(ConfidenceLevel.High, "\"High\"")]
    [TestCase(ConfidenceLevel.Medium, "\"Medium\"")]
    [TestCase(ConfidenceLevel.Low, "\"Low\"")]
    [TestCase(SignalStrength.Strong, "\"Strong\"")]
    [TestCase(SignalStrength.Moderate, "\"Moderate\"")]
    [TestCase(SignalStrength.Weak, "\"Weak\"")]
    public void EnumValue_SerializesAsItsMemberName(object enumValue, string expectedJson)
    {
        var json = JsonSerializer.Serialize(enumValue, KanelJsonOptions.CamelCase);

        Assert.That(json, Is.EqualTo(expectedJson));
    }

    #endregion
}

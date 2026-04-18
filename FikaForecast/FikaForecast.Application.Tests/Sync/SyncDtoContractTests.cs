using System.Text.Json;
using FikaForecast.Application.Sync.Dtos;

namespace FikaForecast.Application.Tests.Sync;

/// <summary>
/// Guards the client-side contract: given JSON shaped exactly like the backend
/// <c>/api/sync/*</c> emits (camelCase keys, enums as strings), the WPF <c>Sync*Run</c>
/// DTOs must deserialize without throwing and surface string enum values verbatim.
/// The fixtures are the minimal shape needed to satisfy the contract. They mirror what
/// <c>SyncApiContractTests</c> on the backend asserts — keep the two in sync.
/// </summary>
[TestFixture]
[TestOf(typeof(SyncRunsResponse<>))]
public class SyncDtoContractTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public void SyncNewsBriefRun_DeserializesFromBackendWireFormat()
    {
        // Arrange
        const string json = """
        {
          "from": "2026-04-15",
          "to": "2026-04-16",
          "count": 1,
          "runs": [
            {
              "runDate": "2026-04-16",
              "runId": "run-abc",
              "createdAt": "2026-04-16T12:00:00+00:00",
              "modelId": "gpt-5.4-mini",
              "status": "Success",
              "durationSeconds": 1.5,
              "inputTokens": 100,
              "outputTokens": 200,
              "totalTokens": 300,
              "deploymentName": "gpt-5.4-mini",
              "mood": "RiskOn",
              "summary": "Markets up",
              "assessments": [
                { "category": "Tech", "headline": "H", "summary": "S", "sentiment": "RiskOn" }
              ]
            }
          ]
        }
        """;

        // Act
        var envelope = JsonSerializer.Deserialize<SyncRunsResponse<SyncNewsBriefRun>>(json, Options);

        // Assert
        Assert.That(envelope, Is.Not.Null);
        Assert.That(envelope!.From, Is.EqualTo("2026-04-15"));
        Assert.That(envelope.To, Is.EqualTo("2026-04-16"));
        Assert.That(envelope.Count, Is.EqualTo(1));
        Assert.That(envelope.Runs, Has.Count.EqualTo(1));

        var run = envelope.Runs[0];
        Assert.That(run.RunId, Is.EqualTo("run-abc"));
        Assert.That(run.ModelId, Is.EqualTo("gpt-5.4-mini"));
        Assert.That(run.Status, Is.EqualTo("Success"),
            "Status must land as a string in the DTO — this is the bug the refactor fixed.");
        Assert.That(run.Mood, Is.EqualTo("RiskOn"));
        Assert.That(run.Assessments[0].Sentiment, Is.EqualTo("RiskOn"));
    }

    [Test]
    public void SyncWeeklySummaryRun_DeserializesFromBackendWireFormat_IncludingConfidenceAndNetMood()
    {
        // Arrange
        const string json = """
        {
          "from": "2026-04-10",
          "to": "2026-04-16",
          "count": 1,
          "runs": [
            {
              "runDate": "2026-04-16",
              "runId": "weekly-001",
              "createdAt": "2026-04-16T12:00:00+00:00",
              "modelId": "gpt-5.4-mini",
              "status": "Success",
              "durationSeconds": 5.0,
              "inputTokens": 400,
              "outputTokens": 500,
              "totalTokens": 900,
              "weekStart": "2026-04-10T00:00:00+00:00",
              "weekEnd": "2026-04-16T23:59:59+00:00",
              "netMood": "RiskOff",
              "moodSummary": "Risk-off week",
              "themes": [
                { "category": "Rates", "summary": "Rising", "confidence": "Medium", "sentiment": "RiskOff" }
              ]
            }
          ]
        }
        """;

        // Act
        var envelope = JsonSerializer.Deserialize<SyncRunsResponse<SyncWeeklySummaryRun>>(json, Options);

        // Assert
        Assert.That(envelope, Is.Not.Null);
        var run = envelope!.Runs[0];
        Assert.That(run.Status, Is.EqualTo("Success"));
        Assert.That(run.NetMood, Is.EqualTo("RiskOff"));
        Assert.That(run.Themes[0].Confidence, Is.EqualTo("Medium"),
            "Backend emits \"Medium\" — mapper translates to WPF Moderate.");
        Assert.That(run.Themes[0].Sentiment, Is.EqualTo("RiskOff"));
    }

    [Test]
    public void SyncSubstitutionChainRun_DeserializesFromBackendWireFormat()
    {
        // Arrange
        const string json = """
        {
          "from": "2026-04-15",
          "to": "2026-04-16",
          "count": 1,
          "runs": [
            {
              "runDate": "2026-04-16",
              "runId": "chain-001",
              "createdAt": "2026-04-16T12:00:00+00:00",
              "modelId": "gpt-5.4-mini",
              "status": "Success",
              "durationSeconds": 2.0,
              "inputTokens": 10,
              "outputTokens": 20,
              "totalTokens": 30,
              "weeklySummaryRunId": "weekly-001",
              "chains": [
                { "capitalFleeing": "Tech", "flowsToward": "Energy", "mechanism": "Rates" }
              ]
            }
          ]
        }
        """;

        // Act
        var envelope = JsonSerializer.Deserialize<SyncRunsResponse<SyncSubstitutionChainRun>>(json, Options);

        // Assert
        var run = envelope!.Runs[0];
        Assert.That(run.Status, Is.EqualTo("Success"));
        Assert.That(run.WeeklySummaryRunId, Is.EqualTo("weekly-001"));
        Assert.That(run.Chains[0].CapitalFleeing, Is.EqualTo("Tech"));
        Assert.That(run.Chains[0].FlowsToward, Is.EqualTo("Energy"));
        Assert.That(run.Chains[0].Mechanism, Is.EqualTo("Rates"));
    }

    [Test]
    public void SyncOpportunityScanRun_DeserializesFromBackendWireFormat_IncludingSignalStrength()
    {
        // Arrange
        const string json = """
        {
          "from": "2026-04-15",
          "to": "2026-04-16",
          "count": 1,
          "runs": [
            {
              "runDate": "2026-04-16",
              "runId": "opp-001",
              "createdAt": "2026-04-16T12:00:00+00:00",
              "modelId": "gpt-5.4-mini",
              "status": "Success",
              "durationSeconds": 3.0,
              "inputTokens": 1,
              "outputTokens": 2,
              "totalTokens": 3,
              "substitutionChainRunId": "chain-001",
              "targets": [
                { "category": "Energy", "signalStrength": "Strong", "rationale": "Rotation intact", "riskCaveat": "OPEC" }
              ]
            }
          ]
        }
        """;

        // Act
        var envelope = JsonSerializer.Deserialize<SyncRunsResponse<SyncOpportunityScanRun>>(json, Options);

        // Assert
        var run = envelope!.Runs[0];
        Assert.That(run.Status, Is.EqualTo("Success"));
        Assert.That(run.SubstitutionChainRunId, Is.EqualTo("chain-001"));
        Assert.That(run.Targets[0].SignalStrength, Is.EqualTo("Strong"),
            "SignalStrength DTO field must be string — backend emits \"Strong\"/\"Moderate\"/\"Weak\".");
    }
}

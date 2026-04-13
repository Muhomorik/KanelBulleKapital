using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Serialization;

namespace KanelBrief.Functions.Tests.Api;

/// <summary>
/// Regression tests for the JSON casing contract shared by every Agent Runs
/// endpoint. The frontend type definitions in frontend/lib/types.ts expect
/// camelCase property names (runId, createdAt, …). A previous bug had the
/// list endpoints fall through to the Functions Worker's default serializer
/// which produces PascalCase, causing the BriefSelector to render "--:--"
/// because <c>brief.createdAt</c> was undefined on the client.
/// </summary>
[TestFixture]
public class AgentRunsApiSerializationTests
{
    [Test]
    public void CamelCase_SerializesNewsBriefRun_UsesCamelCaseProperties()
    {
        // Arrange
        var run = new NewsBriefRun
        {
            RunId = "abc-123",
            RunDate = "2026-04-13",
            CreatedAt = new DateTimeOffset(2026, 4, 13, 8, 0, 11, TimeSpan.Zero),
            ModelId = "gpt-5.4-mini",
            Status = RunStatus.Success,
            Mood = "Mixed",
            Summary = "test",
            Assessments = []
        };

        // Act
        var json = JsonSerializer.Serialize(run, KanelJsonOptions.CamelCase);

        // Assert — camelCase keys must be present
        Assert.That(json, Does.Contain("\"runId\""));
        Assert.That(json, Does.Contain("\"runDate\""));
        Assert.That(json, Does.Contain("\"createdAt\""));
        Assert.That(json, Does.Contain("\"mood\""));

        // Assert — PascalCase keys must NOT be present (frontend would parse these as undefined)
        Assert.That(json, Does.Not.Contain("\"RunId\""));
        Assert.That(json, Does.Not.Contain("\"RunDate\""));
        Assert.That(json, Does.Not.Contain("\"CreatedAt\""));
        Assert.That(json, Does.Not.Contain("\"Mood\""));
    }

    [Test]
    public void CamelCase_SerializesNewsBriefRunList_UsesCamelCaseForEveryItem()
    {
        // Arrange — the list endpoints now return multiple briefs per day (every 4 hours).
        // Every item in the array must itself be camelCase.
        var runs = new List<NewsBriefRun>
        {
            new() { RunId = "r1", RunDate = "2026-04-13", CreatedAt = DateTimeOffset.UtcNow, Mood = "Mixed", Summary = "", Assessments = [] },
            new() { RunId = "r2", RunDate = "2026-04-13", CreatedAt = DateTimeOffset.UtcNow.AddHours(-4), Mood = "RiskOn", Summary = "", Assessments = [] }
        };

        // Act
        var json = JsonSerializer.Serialize(runs, KanelJsonOptions.CamelCase);

        // Assert
        Assert.That(json, Does.Contain("\"runId\":\"r1\""));
        Assert.That(json, Does.Contain("\"runId\":\"r2\""));
        Assert.That(json, Does.Not.Contain("\"RunId\""));
    }
}

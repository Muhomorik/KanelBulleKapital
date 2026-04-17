using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Azure.Data.Tables;

using KanelBrief.Core.Models;
using KanelBrief.Core.Serialization;
using KanelBrief.Functions.Api;

namespace KanelBrief.Functions.Tests.Integration;

/// <summary>
/// Live HTTP tests for <c>/api/sync/*</c>. Marked <see cref="ExplicitAttribute"/> —
/// Test Explorer only runs them when you select them directly.
/// </summary>
/// <remarks>
/// <para><b>To run in Visual Studio 2022 Community:</b></para>
/// <list type="number">
///   <item>
///     Right-click <c>KanelBrief.Functions</c> → <i>Set as Startup Project</i>, press
///     <b>Ctrl+F5</b> (Start Without Debugging — <i>not</i> F5, which locks VS into a
///     debug session and blocks Test Explorer). A console opens listing routes on port
///     <b>7220</b> (from <c>Properties/launchSettings.json</c>). VS auto-starts Azurite
///     in the background when it sees <c>UseDevelopmentStorage=true</c> in
///     <c>local.settings.json</c> — nothing to start manually.
///   </item>
///   <item>
///     Test Explorer → locate <c>SyncEndpointsIntegrationTests</c> → right-click →
///     <i>Run</i>. (Run-All skips them because of <see cref="ExplicitAttribute"/>.)
///   </item>
/// </list>
/// <para><b>Defaults:</b> <c>http://localhost:7220</c>. Bearer token is auto-loaded from
/// <c>KanelBrief.Functions/local.settings.json</c> — whatever <c>SYNC_AUTH_TOKEN</c> the
/// running Function App uses, the test uses too. No manual sync.</para>
/// <para><b>Overrides</b> (for pointing at a staging Function App): set user secrets on
/// <c>KanelBrief.Functions.Tests</c>: <c>SYNC_TEST_BASE_URL</c>, <c>SYNC_TEST_AUTH_TOKEN</c>.</para>
/// <para><b>If the Function App isn't running,</b> the fixture's probe loop bails in ~5s and
/// reports <c>Inconclusive</c> (not Failed).</para>
/// </remarks>
[TestFixture]
[TestOf(typeof(SyncApi))]
[Category("Integration")]
[Explicit("Requires Azurite + running Function App — see class doc for steps.")]
public class SyncEndpointsIntegrationTests
{
    private const string TestPartition = "2026-04-16";
    private const string TestFrom = "2026-04-15";
    private const string TestTo = "2026-04-16";

    // Deterministic IDs so multiple test runs are idempotent (UpsertEntity in seed + ExistsAsync on client).
    private const string NewsBriefRunId = "itest-news-brief-001";
    private const string WeeklySummaryRunId = "itest-weekly-001";
    private const string SubstitutionChainRunId = "itest-chain-001";
    private const string OpportunityScanRunId = "itest-opp-001";

    private string _baseUrl = null!;
    private string _token = null!;
    private HttpClient _http = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var config = IntegrationConfig.Load();
        _baseUrl = IntegrationConfig.GetSyncBaseUrl(config);
        _token = IntegrationConfig.GetSyncAuthToken(config);

        _http = new HttpClient { BaseAddress = new Uri(_baseUrl), Timeout = TimeSpan.FromSeconds(10) };

        await WaitForBackend();
        await SeedAzurite();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _http?.Dispose();
        await CleanupAzurite();
    }

    #region 401 flow

    [Test]
    public async Task SyncNewsBriefs_NoAuth_Returns401()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/sync/news-briefs?from={TestFrom}&to={TestTo}");

        // Act
        var response = await _http.SendAsync(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task SyncNewsBriefs_WrongToken_Returns401()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/sync/news-briefs?from={TestFrom}&to={TestTo}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-the-token");

        // Act
        var response = await _http.SendAsync(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task SyncNewsBriefs_WrongScheme_Returns401()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/sync/news-briefs?from={TestFrom}&to={TestTo}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _token);

        // Act
        var response = await _http.SendAsync(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    #endregion

    #region 400 flow — bad query strings

    [TestCase("bogus", "2026-04-16", "Invalid 'from'")]
    [TestCase("2026-04-15", "bogus", "Invalid 'to'")]
    [TestCase("2026-04-17", "2026-04-16", "'from' must be <= 'to'")]
    public async Task SyncNewsBriefs_BadQueryString_Returns400WithSanitizedError(
        string from, string to, string errorFragment)
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/sync/news-briefs?from={from}&to={to}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        // Act
        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var errorMessage = doc.RootElement.GetProperty("error").GetString();

        // Assert — inspect the decoded 'error' field, not the raw JSON
        // (System.Text.Json escapes ' and — as \u0027 / \u2014 in the wire body).
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(errorMessage, Does.Contain(errorFragment));
    }

    #endregion

    #region Happy path — seeded row flows back through the whole stack

    [Test]
    public async Task SyncNewsBriefs_WithAuth_ReturnsSeededRow_WithEnumStrings()
    {
        // Arrange — seeded row planted by OneTimeSetUp
        // (no per-test arrange needed)

        // Act
        var doc = await FetchSyncJson("news-briefs", TestFrom, TestTo);
        var run = FindRunById(doc, NewsBriefRunId);
        var assessment = run.GetProperty("assessments")[0];

        // Assert
        AssertEnvelopeShape(doc, TestFrom, TestTo, 1);
        Assert.That(run.GetProperty("status").ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(run.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(run.GetProperty("mood").GetString(), Is.EqualTo("RiskOn"));
        Assert.That(assessment.GetProperty("sentiment").ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(assessment.GetProperty("sentiment").GetString(), Is.EqualTo("RiskOn"));
    }

    [Test]
    public async Task SyncWeeklySummaries_WithAuth_ReturnsSeededRow_WithEnumStrings()
    {
        // Arrange
        // (seeded by OneTimeSetUp)

        // Act
        var doc = await FetchSyncJson("weekly-summaries", TestFrom, TestTo);
        var run = FindRunById(doc, WeeklySummaryRunId);
        var theme = run.GetProperty("themes")[0];

        // Assert
        AssertEnvelopeShape(doc, TestFrom, TestTo, 1);
        Assert.That(run.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(run.GetProperty("netMood").GetString(), Is.EqualTo("RiskOff"));
        Assert.That(theme.GetProperty("confidence").GetString(), Is.EqualTo("Medium"));
        Assert.That(theme.GetProperty("sentiment").GetString(), Is.EqualTo("RiskOff"));
    }

    [Test]
    public async Task SyncSubstitutionChains_WithAuth_ReturnsSeededRow()
    {
        // Arrange
        // (seeded by OneTimeSetUp)

        // Act
        var doc = await FetchSyncJson("substitution-chains", TestFrom, TestTo);
        var run = FindRunById(doc, SubstitutionChainRunId);

        // Assert
        AssertEnvelopeShape(doc, TestFrom, TestTo, 1);
        Assert.That(run.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(run.GetProperty("weeklySummaryRunId").GetString(), Is.EqualTo(WeeklySummaryRunId));
    }

    [Test]
    public async Task SyncOpportunityScans_WithAuth_ReturnsSeededRow_WithSignalStrengthString()
    {
        // Arrange
        // (seeded by OneTimeSetUp)

        // Act
        var doc = await FetchSyncJson("opportunity-scans", TestFrom, TestTo);
        var run = FindRunById(doc, OpportunityScanRunId);
        var target = run.GetProperty("targets")[0];

        // Assert
        AssertEnvelopeShape(doc, TestFrom, TestTo, 1);
        Assert.That(run.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(target.GetProperty("signalStrength").ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(target.GetProperty("signalStrength").GetString(), Is.EqualTo("Strong"));
    }

    #endregion

    #region Regression — anonymous endpoints unaffected by middleware

    [Test]
    public async Task GetDashboard_NoAuth_Still200()
    {
        // Arrange — anonymous endpoint, no headers or seed needed.

        // Act
        var response = await _http.GetAsync("/api/dashboard");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Helpers — backend probe, Azurite seed, JSON fetch

    private async Task WaitForBackend()
    {
        // Dedicated probe client with a tight per-call timeout so a silently-unresponsive
        // host can't stall the fixture. Total budget: ~5 seconds across 10 attempts.
        using var probeClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromMilliseconds(400)
        };

        for (var i = 0; i < 10; i++)
        {
            try
            {
                using var probe = await probeClient.GetAsync("/api/dashboard");
                if (probe.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // connection refused / timeout / whatever — retry
            }

            await Task.Delay(100);
        }

        Assert.Inconclusive(
            $"Backend at {_baseUrl} not reachable after 5s. Start the Function App (Ctrl+F5 on KanelBrief.Functions).");
    }

    private async Task<JsonDocument> FetchSyncJson(string segment, string from, string to)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"/api/sync/{segment}?from={from}&to={to}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            $"Expected 200 from /api/sync/{segment}, got {(int)resp.StatusCode}. Body: {body}");

        return JsonDocument.Parse(body);
    }

    private static void AssertEnvelopeShape(JsonDocument doc, string from, string to, int countAtLeast)
    {
        var root = doc.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("from").GetString(), Is.EqualTo(from));
            Assert.That(root.GetProperty("to").GetString(), Is.EqualTo(to));
            Assert.That(root.GetProperty("count").GetInt32(), Is.GreaterThanOrEqualTo(countAtLeast));
            Assert.That(root.GetProperty("runs").GetArrayLength(), Is.GreaterThanOrEqualTo(countAtLeast));
        });
    }

    private static JsonElement FindRunById(JsonDocument doc, string runId)
    {
        foreach (var run in doc.RootElement.GetProperty("runs").EnumerateArray())
        {
            if (run.GetProperty("runId").GetString() == runId)
            {
                return run;
            }
        }

        Assert.Fail($"Seeded run '{runId}' not found in response. Azurite may be empty or on a different port.");
        return default;
    }

    private static TableServiceClient AzuriteClient() => new("UseDevelopmentStorage=true");

    private static async Task SeedAzurite()
    {
        var client = AzuriteClient();

        var newsBrief = client.GetTableClient("NewsBriefRuns");
        await newsBrief.CreateIfNotExistsAsync();
        await newsBrief.UpsertEntityAsync(new TableEntity(TestPartition, NewsBriefRunId)
        {
            { "ModelId", "gpt-5.4-mini" },
            { "Status", "Success" },
            { "DurationSeconds", 1.5 },
            { "InputTokens", 100 },
            { "OutputTokens", 200 },
            { "TotalTokens", 300 },
            { "CreatedAt", DateTimeOffset.Parse("2026-04-16T12:00:00Z") },
            { "DeploymentName", "gpt-5.4-mini" },
            { "Mood", "RiskOn" },
            { "Summary", "Integration seeded brief" },
            {
                "Assessments", JsonSerializer.Serialize(new List<CategoryAssessment>
                {
                    new() { Category = "Tech", Headline = "H", Summary = "S", Sentiment = MarketSentiment.RiskOn }
                }, KanelJsonOptions.CamelCase)
            }
        });

        var weekly = client.GetTableClient("WeeklySummaryRuns");
        await weekly.CreateIfNotExistsAsync();
        await weekly.UpsertEntityAsync(new TableEntity(TestPartition, WeeklySummaryRunId)
        {
            { "ModelId", "gpt-5.4-mini" },
            { "Status", "Success" },
            { "DurationSeconds", 5.0 },
            { "InputTokens", 400 },
            { "OutputTokens", 500 },
            { "TotalTokens", 900 },
            { "CreatedAt", DateTimeOffset.Parse("2026-04-16T12:00:00Z") },
            { "WeekStart", DateTimeOffset.Parse("2026-04-10T00:00:00Z") },
            { "WeekEnd", DateTimeOffset.Parse("2026-04-16T23:59:59Z") },
            { "NetMood", "RiskOff" },
            { "MoodSummary", "Risk-off week" },
            {
                "Themes", JsonSerializer.Serialize(new List<WeeklySummaryTheme>
                {
                    new()
                    {
                        Category = "Rates", Summary = "Rising", Confidence = ConfidenceLevel.Medium,
                        Sentiment = MarketSentiment.RiskOff
                    }
                }, KanelJsonOptions.CamelCase)
            }
        });

        var chains = client.GetTableClient("SubstitutionChainRuns");
        await chains.CreateIfNotExistsAsync();
        await chains.UpsertEntityAsync(new TableEntity(TestPartition, SubstitutionChainRunId)
        {
            { "ModelId", "gpt-5.4-mini" },
            { "Status", "Success" },
            { "DurationSeconds", 2.0 },
            { "InputTokens", 10 },
            { "OutputTokens", 20 },
            { "TotalTokens", 30 },
            { "CreatedAt", DateTimeOffset.Parse("2026-04-16T12:00:00Z") },
            { "WeeklySummaryRunId", WeeklySummaryRunId },
            {
                "Chains", JsonSerializer.Serialize(new List<RotationChain>
                {
                    new() { CapitalFleeing = "Tech", FlowsToward = "Energy", Mechanism = "Rates" }
                }, KanelJsonOptions.CamelCase)
            }
        });

        var opps = client.GetTableClient("OpportunityScanRuns");
        await opps.CreateIfNotExistsAsync();
        await opps.UpsertEntityAsync(new TableEntity(TestPartition, OpportunityScanRunId)
        {
            { "ModelId", "gpt-5.4-mini" },
            { "Status", "Success" },
            { "DurationSeconds", 3.0 },
            { "InputTokens", 1 },
            { "OutputTokens", 2 },
            { "TotalTokens", 3 },
            { "CreatedAt", DateTimeOffset.Parse("2026-04-16T12:00:00Z") },
            { "SubstitutionChainRunId", SubstitutionChainRunId },
            {
                "Targets", JsonSerializer.Serialize(new List<RotationTarget>
                {
                    new()
                    {
                        Category = "Energy", SignalStrength = SignalStrength.Strong, Rationale = "Rotation intact",
                        RiskCaveat = "OPEC"
                    }
                }, KanelJsonOptions.CamelCase)
            }
        });
    }

    private static async Task CleanupAzurite()
    {
        var client = AzuriteClient();
        await TryDelete(client.GetTableClient("NewsBriefRuns"), TestPartition, NewsBriefRunId);
        await TryDelete(client.GetTableClient("WeeklySummaryRuns"), TestPartition, WeeklySummaryRunId);
        await TryDelete(client.GetTableClient("SubstitutionChainRuns"), TestPartition, SubstitutionChainRunId);
        await TryDelete(client.GetTableClient("OpportunityScanRuns"), TestPartition, OpportunityScanRunId);
    }

    private static async Task TryDelete(TableClient table, string partition, string rowKey)
    {
        try
        {
            await table.DeleteEntityAsync(partition, rowKey);
        }
        catch
        {
            // Best-effort cleanup; ignore failures (table may not exist if backend never ran).
        }
    }

    #endregion
}
using KanelBrief.Functions.Api;

namespace KanelBrief.Functions.Tests.Api;

[TestFixture]
[TestOf(typeof(AgentRunsApi))]
public class FindLatestAsyncTests
{
    [Test]
    public async Task FindLatestAsync_RunsExistToday_ReturnsImmediately()
    {
        var todayData = new List<string> { "run-1", "run-2" };
        var callCount = 0;

        var result = await AgentRunsApi.FindLatestAsync<string>(date =>
        {
            callCount++;
            return Task.FromResult(callCount == 1 ? todayData : new List<string>());
        });

        Assert.That(result, Is.EqualTo(todayData));
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public async Task FindLatestAsync_NoRunsToday_FindsYesterday()
    {
        var yesterdayData = new List<string> { "run-yesterday" };
        var callCount = 0;

        var result = await AgentRunsApi.FindLatestAsync<string>(date =>
        {
            callCount++;
            return Task.FromResult(callCount == 2 ? yesterdayData : new List<string>());
        });

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("run-yesterday"));
        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public async Task FindLatestAsync_NoRunsFor6Days_FindsDay7()
    {
        var day7Data = new List<string> { "old-run" };
        var callCount = 0;

        var result = await AgentRunsApi.FindLatestAsync<string>(date =>
        {
            callCount++;
            return Task.FromResult(callCount == 7 ? day7Data : new List<string>());
        });

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(callCount, Is.EqualTo(7));
    }

    [Test]
    public async Task FindLatestAsync_NoRunsFor7Days_ReturnsEmptyList()
    {
        var callCount = 0;

        var result = await AgentRunsApi.FindLatestAsync<string>(date =>
        {
            callCount++;
            return Task.FromResult(new List<string>());
        });

        Assert.That(result, Is.Empty);
        Assert.That(callCount, Is.EqualTo(7));
    }

    [Test]
    public async Task FindLatestAsync_MultipleRunsOnDay_ReturnsAll()
    {
        var multipleRuns = new List<string> { "a", "b", "c" };

        var result = await AgentRunsApi.FindLatestAsync<string>(date =>
            Task.FromResult(multipleRuns));

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task FindLatestAsync_StopsSearchingAfterFirstMatch()
    {
        var callCount = 0;

        await AgentRunsApi.FindLatestAsync<string>(date =>
        {
            callCount++;
            return Task.FromResult(new List<string> { "found" });
        });

        Assert.That(callCount, Is.EqualTo(1), "Should stop after first non-empty result");
    }

    [Test]
    public async Task FindLatestAsync_SearchesMaximum7Days()
    {
        var callCount = 0;

        await AgentRunsApi.FindLatestAsync<string>(date =>
        {
            callCount++;
            return Task.FromResult(new List<string>());
        });

        Assert.That(callCount, Is.EqualTo(7), "Should search exactly 7 days");
    }
}

using KanelBrief.Functions.Middleware;

namespace KanelBrief.Functions.Tests.Middleware;

[TestFixture]
[TestOf(typeof(RateLimitMiddleware))]
public class RateLimitMiddlewareTests
{
    #region ShouldSkip — Sync-prefixed functions bypass the rate limit

    [TestCase("SyncNewsBriefRuns")]
    [TestCase("SyncWeeklySummaryRuns")]
    [TestCase("SyncSubstitutionChainRuns")]
    [TestCase("SyncOpportunityScanRuns")]
    [TestCase("Sync")]
    [TestCase("SyncAnything")]
    public void ShouldSkip_FunctionNameStartsWithSync_ReturnsTrue(string functionName)
    {
        // Act
        var result = RateLimitMiddleware.ShouldSkip(functionName);

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("GetDashboard")]
    [TestCase("GetNewsBriefRuns")]
    [TestCase("RunNewsBriefAgent")]
    [TestCase("sync")]  // lowercase must NOT match — prefix is ordinal
    [TestCase("")]
    [TestCase("DailyPipelineOrchestrator")]
    public void ShouldSkip_NonSyncPrefix_ReturnsFalse(string functionName)
    {
        // Act
        var result = RateLimitMiddleware.ShouldSkip(functionName);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region ExtractClientIp — X-Forwarded-For parsing

    [Test]
    public void ExtractClientIp_SingleIp_ReturnsIt()
    {
        // Act
        var result = RateLimitMiddleware.ExtractClientIp("1.2.3.4");

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3.4"));
    }

    [Test]
    public void ExtractClientIp_ClientAndProxyChain_ReturnsFirstEntry()
    {
        // Azure frontend appends upstream proxies after the origin client.
        // Act
        var result = RateLimitMiddleware.ExtractClientIp("1.2.3.4, 10.0.0.1, 10.0.0.2");

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3.4"));
    }

    [Test]
    public void ExtractClientIp_PaddedWhitespace_IsTrimmed()
    {
        // Act
        var result = RateLimitMiddleware.ExtractClientIp("  1.2.3.4  ");

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3.4"));
    }

    [Test]
    public void ExtractClientIp_IPv6_ReturnsAsIs()
    {
        // Act
        var result = RateLimitMiddleware.ExtractClientIp("2001:db8::1");

        // Assert
        Assert.That(result, Is.EqualTo("2001:db8::1"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ExtractClientIp_MissingOrBlank_ReturnsUnknown(string? header)
    {
        // Act
        var result = RateLimitMiddleware.ExtractClientIp(header);

        // Assert
        Assert.That(result, Is.EqualTo("unknown"));
    }

    #endregion

    #region IsOverLimit — sliding window core

    [Test]
    public void IsOverLimit_EmptyQueue_AppendsAndReturnsFalse()
    {
        // Arrange
        var queue = new Queue<DateTimeOffset>();
        var now = DateTimeOffset.UtcNow;

        // Act
        var result = RateLimitMiddleware.IsOverLimit(queue, now, TimeSpan.FromSeconds(60), limit: 3);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(queue, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void IsOverLimit_BelowLimit_AppendsAndReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var queue = new Queue<DateTimeOffset>([now.AddSeconds(-10), now.AddSeconds(-5)]);

        // Act
        var result = RateLimitMiddleware.IsOverLimit(queue, now, TimeSpan.FromSeconds(60), limit: 3);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(queue, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void IsOverLimit_AtLimit_DoesNotAppendAndReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var queue = new Queue<DateTimeOffset>(
            [now.AddSeconds(-30), now.AddSeconds(-20), now.AddSeconds(-10)]);

        // Act
        var result = RateLimitMiddleware.IsOverLimit(queue, now, TimeSpan.FromSeconds(60), limit: 3);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(queue, Has.Count.EqualTo(3), "Over-limit calls must not be appended");
        });
    }

    [Test]
    public void IsOverLimit_EntriesOutsideWindow_ArePrunedAndRequestAllowed()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var queue = new Queue<DateTimeOffset>(
            [now.AddSeconds(-120), now.AddSeconds(-90), now.AddSeconds(-5)]);

        // Act
        var result = RateLimitMiddleware.IsOverLimit(queue, now, TimeSpan.FromSeconds(60), limit: 3);

        // Assert
        // Stale entries dropped (-120, -90), fresh (-5) kept, new `now` appended → size 2.
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(queue, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void IsOverLimit_AllEntriesStale_RequestAllowed()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var queue = new Queue<DateTimeOffset>(
            [now.AddSeconds(-300), now.AddSeconds(-200), now.AddSeconds(-100)]);

        // Act
        var result = RateLimitMiddleware.IsOverLimit(queue, now, TimeSpan.FromSeconds(60), limit: 3);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(queue, Has.Count.EqualTo(1), "Stale entries pruned, only new `now` remains");
        });
    }

    [Test]
    public void IsOverLimit_ExactlyAtCutoff_StillStale()
    {
        // Arrange — cutoff is strict `<`, so an entry exactly at `now - window` is dropped.
        var now = DateTimeOffset.UtcNow;
        var window = TimeSpan.FromSeconds(60);
        var queue = new Queue<DateTimeOffset>([now - window - TimeSpan.FromTicks(1)]);

        // Act
        var result = RateLimitMiddleware.IsOverLimit(queue, now, window, limit: 1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(queue, Has.Count.EqualTo(1), "Stale entry pruned, new entry added");
        });
    }

    #endregion
}

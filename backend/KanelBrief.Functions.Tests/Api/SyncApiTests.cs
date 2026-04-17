using KanelBrief.Functions.Api;

namespace KanelBrief.Functions.Tests.Api;

[TestFixture]
[TestOf(typeof(SyncApi))]
public class SyncApiTests
{
    [Test]
    public void TryParseRange_ValidDates_ReturnsTrueAndPopulatesOutputs()
    {
        // Arrange
        const string fromRaw = "2026-04-15";
        const string toRaw = "2026-04-16";

        // Act
        var ok = SyncApi.TryParseRange(fromRaw, toRaw, out var from, out var to, out var error);

        // Assert
        Assert.That(ok, Is.True);
        Assert.That(from, Is.EqualTo("2026-04-15"));
        Assert.That(to, Is.EqualTo("2026-04-16"));
        Assert.That(error, Is.Empty);
    }

    [Test]
    public void TryParseRange_SameFromAndTo_Accepted()
    {
        // Arrange
        const string same = "2026-04-16";

        // Act
        var ok = SyncApi.TryParseRange(same, same, out _, out _, out var error);

        // Assert
        Assert.That(ok, Is.True);
        Assert.That(error, Is.Empty);
    }

    [Test]
    public void TryParseRange_FromGreaterThanTo_ReturnsFalse()
    {
        // Arrange
        const string fromRaw = "2026-04-17";
        const string toRaw = "2026-04-16";

        // Act
        var ok = SyncApi.TryParseRange(fromRaw, toRaw, out _, out _, out var error);

        // Assert
        Assert.That(ok, Is.False);
        Assert.That(error, Is.EqualTo("'from' must be <= 'to'"));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("2026-4-15")]
    [TestCase("04/15/2026")]
    [TestCase("not-a-date")]
    public void TryParseRange_InvalidFrom_ReturnsFalseWithSanitizedError(string? bad)
    {
        // Arrange
        const string validTo = "2026-04-16";

        // Act
        var ok = SyncApi.TryParseRange(bad, validTo, out _, out _, out var error);

        // Assert
        Assert.That(ok, Is.False);
        Assert.That(error, Is.EqualTo("Invalid 'from' — expected yyyy-MM-dd"),
            "Error must be a fixed string — never echo user input.");
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("2026-13-01")]
    [TestCase("bogus")]
    public void TryParseRange_InvalidTo_ReturnsFalseWithSanitizedError(string? bad)
    {
        // Arrange
        const string validFrom = "2026-04-15";

        // Act
        var ok = SyncApi.TryParseRange(validFrom, bad, out _, out _, out var error);

        // Assert
        Assert.That(ok, Is.False);
        Assert.That(error, Is.EqualTo("Invalid 'to' — expected yyyy-MM-dd"));
    }

    [Test]
    public void TryParseRange_ErrorNeverEchoesUserSuppliedValue()
    {
        // Arrange — attacker payload includes script + bogus credentials
        const string attacker = "<script>alert('pwn')</script>Bearer sk_live_xxx";

        // Act
        var ok = SyncApi.TryParseRange(attacker, "2026-04-16", out _, out _, out var error);

        // Assert
        Assert.That(ok, Is.False);
        Assert.That(error, Does.Not.Contain(attacker));
        Assert.That(error, Does.Not.Contain("sk_live"));
    }
}

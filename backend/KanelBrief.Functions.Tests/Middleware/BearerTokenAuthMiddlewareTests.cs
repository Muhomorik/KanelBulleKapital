using KanelBrief.Functions.Middleware;

namespace KanelBrief.Functions.Tests.Middleware;

[TestFixture]
[TestOf(typeof(BearerTokenAuthMiddleware))]
public class BearerTokenAuthMiddlewareTests
{
    #region RequiresAuth — opt-in by function-name prefix

    [TestCase("SyncNewsBriefRuns")]
    [TestCase("SyncWeeklySummaryRuns")]
    [TestCase("SyncSubstitutionChainRuns")]
    [TestCase("SyncOpportunityScanRuns")]
    [TestCase("Sync")]
    [TestCase("SyncAnything")]
    public void RequiresAuth_FunctionNameStartsWithSync_ReturnsTrue(string functionName)
    {
        // Arrange
        // (functionName provided by TestCase)

        // Act
        var result = BearerTokenAuthMiddleware.RequiresAuth(functionName);

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("GetDashboard")]
    [TestCase("GetNewsBriefRuns")]
    [TestCase("RunNewsBriefAgent")]
    [TestCase("sync")]  // lowercase must NOT match — prefix is ordinal
    [TestCase("")]
    [TestCase("DailyPipelineOrchestrator")]
    public void RequiresAuth_NonSyncPrefix_ReturnsFalse(string functionName)
    {
        // Arrange
        // (functionName provided by TestCase)

        // Act
        var result = BearerTokenAuthMiddleware.RequiresAuth(functionName);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region IsBearerValid — pure predicate

    [Test]
    public void IsBearerValid_MatchingToken_ReturnsTrue()
    {
        // Arrange
        const string expected = "dev-token-change-me";
        const string header = "Bearer dev-token-change-me";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, expected);

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    public void IsBearerValid_MissingHeader_ReturnsFalse(string? header)
    {
        // Arrange
        const string expected = "any-token";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, expected);

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase("dev-token-change-me")]           // no scheme prefix
    [TestCase("Basic dev-token-change-me")]     // wrong scheme
    [TestCase("bearer dev-token-change-me")]    // case-sensitive: lowercase rejected
    [TestCase("BEARER dev-token-change-me")]    // case-sensitive: uppercase rejected
    public void IsBearerValid_WrongScheme_ReturnsFalse(string header)
    {
        // Arrange
        const string expected = "dev-token-change-me";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, expected);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsBearerValid_CorrectSchemeWrongToken_ReturnsFalse()
    {
        // Arrange
        const string header = "Bearer wrong-token";
        const string expected = "right-token";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, expected);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsBearerValid_EmptyTokenValueAfterBearer_ReturnsFalse()
    {
        // Arrange
        const string header = "Bearer ";
        const string expected = "some-token";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, expected);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsBearerValid_TokenShorterThanExpected_ReturnsFalse()
    {
        // Length-mismatch short-circuit is still safe — an attacker learns only the
        // expected length, acceptable for a 32-byte shared secret.
        // Arrange
        const string header = "Bearer aaaa";
        const string expected = "aaaaaaaa";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, expected);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsBearerValid_TokenLongerThanExpected_ReturnsFalse()
    {
        // Arrange
        const string header = "Bearer aaaaaaaaaaaa";
        const string expected = "aaaaaaaa";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, expected);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsBearerValid_SingleCharacterDifference_ReturnsFalse()
    {
        // Arrange
        const string header = "Bearer abcdefgh";
        const string expected = "abcdefgi";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, expected);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsBearerValid_ExactMatchWithSpecialCharacters_ReturnsTrue()
    {
        // Arrange
        const string token = "aB3!@#$%^&*()_+-=[]{}|;:,.<>?/~`abcdef0123456789";
        const string header = "Bearer aB3!@#$%^&*()_+-=[]{}|;:,.<>?/~`abcdef0123456789";

        // Act
        var result = BearerTokenAuthMiddleware.IsBearerValid(header, token);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion
}

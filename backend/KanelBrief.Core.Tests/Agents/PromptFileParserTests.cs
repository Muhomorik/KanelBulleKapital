using KanelBrief.Core.Agents;

namespace KanelBrief.Core.Tests.Agents;

[TestFixture]
[TestOf(typeof(PromptFileParser))]
public class PromptFileParserTests
{
    [Test]
    public void Parse_StandardFrontmatter_ReturnsNameAndBody()
    {
        var content = "---\nName: My Agent\n---\nYou are a helpful agent.";

        var (name, body) = PromptFileParser.Parse(content, fallbackName: "fallback");

        Assert.That(name, Is.EqualTo("My Agent"));
        Assert.That(body, Is.EqualTo("You are a helpful agent."));
    }

    [Test]
    public void Parse_FrontmatterWithoutNameField_UsesFallbackName()
    {
        var content = "---\nVersion: 1.2\n---\nBody text.";

        var (name, body) = PromptFileParser.Parse(content, fallbackName: "fallback-key");

        Assert.That(name, Is.EqualTo("fallback-key"));
        Assert.That(body, Is.EqualTo("Body text."));
    }

    [Test]
    public void Parse_NoFrontmatter_ReturnsFallbackNameAndFullContent()
    {
        var content = "You are an agent.\nDo things.";

        var (name, body) = PromptFileParser.Parse(content, fallbackName: "default");

        Assert.That(name, Is.EqualTo("default"));
        Assert.That(body, Is.EqualTo("You are an agent.\nDo things."));
    }

    [Test]
    public void Parse_FrontmatterMissingClosingDelimiter_ReturnsFallbackNameAndFullContent()
    {
        var content = "---\nName: Stuck\nBody never closes the frontmatter block";

        var (name, body) = PromptFileParser.Parse(content, fallbackName: "fallback");

        Assert.That(name, Is.EqualTo("fallback"));
        Assert.That(body, Does.Contain("Name: Stuck"));
    }

    [Test]
    public void Parse_CrlfLineEndings_ParsesCorrectly()
    {
        var content = "---\r\nName: CRLF Agent\r\n---\r\nBody line one.\r\nBody line two.";

        var (name, body) = PromptFileParser.Parse(content, fallbackName: "fallback");

        Assert.That(name, Is.EqualTo("CRLF Agent"));
        Assert.That(body, Does.Contain("Body line one."));
        Assert.That(body, Does.Contain("Body line two."));
    }

    [Test]
    public void Parse_BodyWithSurroundingWhitespace_TrimsBody()
    {
        var content = "---\nName: X\n---\n\n\n   You are X.   \n\n";

        var (_, body) = PromptFileParser.Parse(content, fallbackName: "fallback");

        Assert.That(body, Is.EqualTo("You are X."));
    }

    [Test]
    public void Parse_NameWithSurroundingWhitespace_TrimsName()
    {
        var content = "---\nName:    Spaced Name    \n---\nBody.";

        var (name, _) = PromptFileParser.Parse(content, fallbackName: "fallback");

        Assert.That(name, Is.EqualTo("Spaced Name"));
    }

    [Test]
    public void Parse_FrontmatterCaseInsensitiveNameKey_StillFindsName()
    {
        var content = "---\nname: lowercase key\n---\nBody.";

        var (name, _) = PromptFileParser.Parse(content, fallbackName: "fallback");

        Assert.That(name, Is.EqualTo("lowercase key"));
    }

    [Test]
    public void Parse_MultilineBody_PreservesInternalNewlines()
    {
        var content = "---\nName: Multi\n---\nLine 1.\nLine 2.\nLine 3.";

        var (_, body) = PromptFileParser.Parse(content, fallbackName: "fallback");

        Assert.That(body, Is.EqualTo("Line 1.\nLine 2.\nLine 3."));
    }

    [Test]
    public void Parse_NullContent_ThrowsArgumentNullException()
    {
        Assert.That(
            () => PromptFileParser.Parse(null!, fallbackName: "fallback"),
            Throws.TypeOf<ArgumentNullException>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Parse_NullOrWhitespaceFallback_ThrowsArgumentException(string? fallback)
    {
        Assert.That(
            () => PromptFileParser.Parse("anything", fallbackName: fallback!),
            Throws.InstanceOf<ArgumentException>());
    }
}

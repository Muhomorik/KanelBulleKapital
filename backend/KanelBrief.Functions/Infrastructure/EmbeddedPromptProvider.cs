using System.Collections.Concurrent;
using System.Reflection;
using KanelBrief.Core.Agents;

namespace KanelBrief.Functions.Infrastructure;

/// <summary>
/// Loads agent prompts from embedded resources in <c>KanelBrief.Core</c> and caches them
/// for the lifetime of the Function App. Resource layout: <c>KanelBrief.Core.Prompts.{key}.prompt.txt</c>.
/// </summary>
public sealed class EmbeddedPromptProvider : IPromptProvider
{
    private const string ResourcePrefix = "KanelBrief.Core.Prompts.";
    private const string ResourceSuffix = ".prompt.txt";

    private const string WeeklySummaryKey = "weeklysummary";
    private const string SubstitutionChainKey = "substitutionchain";
    private const string OpportunityScanKey = "opportunityscan";
    private const string NewsBriefArticlesKey = "newsbriefarticles";

    private readonly Assembly _resourceAssembly = typeof(IPromptProvider).Assembly;
    private readonly ConcurrentDictionary<string, AgentPrompt> _cache = new();

    public AgentPrompt GetWeeklySummaryPrompt() => Load(WeeklySummaryKey);

    public AgentPrompt GetSubstitutionChainPrompt() => Load(SubstitutionChainKey);

    public AgentPrompt GetOpportunityScanPrompt() => Load(OpportunityScanKey);

    public AgentPrompt GetNewsBriefArticlesPrompt() => Load(NewsBriefArticlesKey);

    private AgentPrompt Load(string key) =>
        _cache.GetOrAdd(key, k =>
        {
            var resourceName = $"{ResourcePrefix}{k}{ResourceSuffix}";
            using var stream = _resourceAssembly.GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                var available = string.Join(", ", _resourceAssembly.GetManifestResourceNames());
                throw new InvalidOperationException(
                    $"Embedded prompt resource '{resourceName}' not found in {_resourceAssembly.GetName().Name}. " +
                    $"Available: [{available}]. Check that Prompts\\*.prompt.txt is registered as <EmbeddedResource> " +
                    "in KanelBrief.Core.csproj.");
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            var (name, systemPrompt) = PromptFileParser.Parse(content, fallbackName: k);
            return new AgentPrompt(name, systemPrompt);
        });
}

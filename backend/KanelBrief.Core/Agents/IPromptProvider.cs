namespace KanelBrief.Core.Agents;

/// <summary>
/// Provides agent prompts that encode domain rules (sourcing tiers, sentiment definitions,
/// signal-strength criteria). Backed by embedded resources shipped with the Core assembly.
/// </summary>
/// <remarks>
/// The News Brief production prompt is NOT served from here — it lives in the Foundry portal
/// (persistent agent <c>kanelbrief-news-brief</c>) so portal-native continuous evaluation can
/// attach to a stable named agent. See <c>docs/AZURE-DEPLOYMENT.md § Persistent Agent Setup</c>.
/// </remarks>
public interface IPromptProvider
{
    /// <summary>Returns the Weekly Summary aggregator prompt.</summary>
    AgentPrompt GetWeeklySummaryPrompt();

    /// <summary>Returns the Substitution Chain rotation-analysis prompt.</summary>
    AgentPrompt GetSubstitutionChainPrompt();

    /// <summary>Returns the Opportunity Scan target-scoring prompt.</summary>
    AgentPrompt GetOpportunityScanPrompt();

    /// <summary>
    /// Returns the article-driven News Brief analysis prompt — used by the
    /// HTTP-triggered <c>POST /api/news-brief</c> endpoint, NOT the timer
    /// (which calls the portal-managed <c>kanelbrief-news-brief</c> agent).
    /// </summary>
    AgentPrompt GetNewsBriefArticlesPrompt();
}

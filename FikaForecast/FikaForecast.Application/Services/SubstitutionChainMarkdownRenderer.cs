using System.Text;
using FikaForecast.Application.DTOs;

namespace FikaForecast.Application.Services;

/// <summary>
/// Renders a <see cref="SubstitutionChainParseResult"/> into display markdown with emojis
/// for the WebView2 UI. Shows capital rotation flows with directional arrows.
/// </summary>
public class SubstitutionChainMarkdownRenderer
{
    /// <summary>
    /// Renders the parse result as emoji markdown showing rotation chains.
    /// </summary>
    public string Render(
        SubstitutionChainParseResult parseResult,
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd)
    {
        if (parseResult.Chains.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var chain in parseResult.Chains)
        {
            sb.AppendLine($"🔴 **Fleeing:** {chain.CapitalFleeing}");
            sb.AppendLine();
            sb.AppendLine($"🟢 **Toward:** {chain.FlowsToward}");
            sb.AppendLine();
            sb.AppendLine($"> **Mechanism:** {chain.Mechanism}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}

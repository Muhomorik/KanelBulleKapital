using System.Text;
using FikaForecast.Application.DTOs;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.Services;

/// <summary>
/// Renders an <see cref="OpportunityScanParseResult"/> into display markdown with emojis
/// for the WebView2 UI. Shows ranked rotation targets with signal strength indicators.
/// </summary>
public class OpportunityScanMarkdownRenderer
{
    /// <summary>
    /// Renders the parse result as emoji markdown showing rotation targets.
    /// </summary>
    public string Render(OpportunityScanParseResult parseResult)
    {
        if (parseResult.Targets.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var target in parseResult.Targets)
        {
            var emoji = target.SignalStrength == SignalStrength.Strong ? "🟢" : "🟡";
            var strength = target.SignalStrength == SignalStrength.Strong ? "Strong" : "Moderate";

            sb.AppendLine($"🎯 **{target.Category}**");
            sb.AppendLine();
            sb.AppendLine($"{emoji} Signal: {strength}");
            sb.AppendLine();
            sb.AppendLine($"> **Rationale:** {target.Rationale}");
            sb.AppendLine();
            sb.AppendLine($"> ⚠️ **Risk:** {target.RiskCaveat}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}

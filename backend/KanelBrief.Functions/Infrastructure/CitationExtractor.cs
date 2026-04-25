using System.Diagnostics.CodeAnalysis;
using KanelBrief.Core.Models;
using Microsoft.Agents.AI;
using OpenAI.Responses;

namespace KanelBrief.Functions.Infrastructure;

/// <summary>
/// Extracts URL citations from a Foundry agent response's annotation channel.
/// </summary>
/// <remarks>
/// Returns an empty list for JSON-mode agent outputs — Foundry only attaches
/// annotations to natural-language prose. See docs/AZURE-DEPLOYMENT.md.
/// </remarks>
[Experimental("OPENAI001")]
internal static class CitationExtractor
{
    public static List<Citation> Extract(AgentResponse response)
    {
        var citations = new List<Citation>();

        if (response.RawRepresentation is not ResponseResult openAiResponse)
            return citations;

        foreach (var item in openAiResponse.OutputItems)
        {
            if (item is not MessageResponseItem messageItem)
                continue;

            foreach (var content in messageItem.Content)
            {
                foreach (var annotation in content.OutputTextAnnotations)
                {
                    if (annotation is UriCitationMessageAnnotation uri)
                    {
                        citations.Add(new Citation(
                            Title: uri.Title ?? string.Empty,
                            Url: uri.Uri?.ToString() ?? string.Empty,
                            StartIndex: (int)uri.StartIndex,
                            EndIndex: (int)uri.EndIndex));
                    }
                }
            }
        }

        return citations;
    }
}

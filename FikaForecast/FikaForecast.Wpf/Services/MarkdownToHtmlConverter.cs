using Markdig;

namespace FikaForecast.Wpf.Services;

/// <summary>
/// Converts Markdown to HTML using Markdig.
/// </summary>
public static class MarkdownToHtmlConverter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string Convert(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return WrapWithStyling("<p><em>No content available</em></p>");

        var html = Markdown.ToHtml(markdown, Pipeline);
        return WrapWithStyling(html);
    }

    private static string WrapWithStyling(string html)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <style>
                    body {
                        font-family: 'Segoe UI', 'Cascadia Code', monospace;
                        font-size: 14px;
                        line-height: 1.6;
                        color: #333;
                        margin: 0;
                        padding: 12px;
                        background-color: #fafafa;
                    }

                    h1, h2, h3, h4, h5, h6 {
                        margin-top: 12px;
                        margin-bottom: 8px;
                        font-weight: 600;
                    }

                    h1 { font-size: 24px; }
                    h2 { font-size: 20px; }
                    h3 { font-size: 18px; }

                    p {
                        margin: 8px 0;
                    }

                    code {
                        background-color: #f0f0f0;
                        padding: 2px 6px;
                        border-radius: 3px;
                        font-family: 'Cascadia Code', monospace;
                        font-size: 13px;
                    }

                    pre {
                        background-color: #2d2d2d;
                        color: #f8f8f2;
                        padding: 12px;
                        border-radius: 4px;
                        overflow-x: auto;
                        margin: 8px 0;
                    }

                    pre code {
                        background: none;
                        padding: 0;
                        color: inherit;
                    }

                    ul, ol {
                        margin: 8px 0;
                        padding-left: 24px;
                    }

                    li {
                        margin: 4px 0;
                    }

                    blockquote {
                        border-left: 4px solid #ccc;
                        padding-left: 12px;
                        margin-left: 0;
                        color: #666;
                        font-style: italic;
                    }

                    a {
                        color: #0078d4;
                        text-decoration: none;
                    }

                    a:hover {
                        text-decoration: underline;
                    }

                    strong {
                        font-weight: 600;
                    }

                    em {
                        font-style: italic;
                    }

                    table {
                        border-collapse: collapse;
                        margin: 12px 0;
                        width: 100%;
                    }

                    th, td {
                        border: 1px solid #ddd;
                        padding: 8px;
                        text-align: left;
                    }

                    th {
                        background-color: #f0f0f0;
                        font-weight: 600;
                    }
                </style>
            </head>
            <body>
                {{html}}
            </body>
            </html>
            """;
    }
}

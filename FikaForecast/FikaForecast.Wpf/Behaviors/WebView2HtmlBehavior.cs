using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Windows;
using System.Windows.Interactivity;

namespace FikaForecast.Wpf.Behaviors;

/// <summary>
/// Behavior that binds markdown content to a WebView2 control and renders it as HTML.
/// </summary>
public class WebView2HtmlBehavior : Behavior<WebView2>
{
    private static CoreWebView2Environment? _sharedEnvironment;
    private static readonly SemaphoreSlim _envLock = new(1, 1);

    private string? _pendingMarkdown;
    private bool _isInitialized;

    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(WebView2HtmlBehavior),
            new PropertyMetadata(null, OnMarkdownChanged));

    public string? Markdown
    {
        get => (string?)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.NavigationCompleted += WebView_NavigationCompleted;

        // In a DataTemplate, the binding may resolve before OnAttached fires,
        // so OnMarkdownChanged silently drops the value (AssociatedObject was null).
        // Capture it here so it renders after initialization.
        if (Markdown is not null)
        {
            _pendingMarkdown = Markdown;
        }

        _ = InitializeWebViewAsync();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.NavigationCompleted -= WebView_NavigationCompleted;
        base.OnDetaching();
    }

    private static async Task<CoreWebView2Environment> GetSharedEnvironmentAsync()
    {
        if (_sharedEnvironment != null)
            return _sharedEnvironment;

        await _envLock.WaitAsync();
        try
        {
            _sharedEnvironment ??= await CoreWebView2Environment.CreateAsync();
            return _sharedEnvironment;
        }
        finally
        {
            _envLock.Release();
        }
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var env = await GetSharedEnvironmentAsync();
            await AssociatedObject.EnsureCoreWebView2Async(env);
            _isInitialized = true;

            // Render any pending markdown that arrived before initialization
            if (_pendingMarkdown != null)
            {
                RenderMarkdown(_pendingMarkdown);
                _pendingMarkdown = null;
            }
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Error(ex, "Failed to initialize WebView2");
        }
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebView2HtmlBehavior behavior)
        {
            behavior.UpdateWebViewContent();
        }
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Navigation completed, content is ready
    }

    private void UpdateWebViewContent()
    {
        if (AssociatedObject == null)
            return;

        if (_isInitialized)
        {
            RenderMarkdown(Markdown);
        }
        else
        {
            // Store the markdown to render once initialization is complete
            _pendingMarkdown = Markdown;
        }
    }

    private void RenderMarkdown(string? markdown)
    {
        var html = Services.MarkdownToHtmlConverter.Convert(markdown);
        AssociatedObject.NavigateToString(html);
    }
}

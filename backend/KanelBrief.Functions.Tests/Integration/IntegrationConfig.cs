using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace KanelBrief.Functions.Tests.Integration;

/// <summary>
/// Shared configuration loader for integration tests.
/// Secrets are expected to be set via <c>dotnet user-secrets</c>
/// on either <c>KanelBrief.Core.Tests</c> or <c>KanelBrief.Functions.Tests</c> (they share a UserSecretsId).
/// </summary>
internal static class IntegrationConfig
{
    public static IConfigurationRoot Load() =>
        new ConfigurationBuilder()
            .AddUserSecrets(typeof(IntegrationConfig).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

    public static Uri RequireFoundryEndpoint(IConfiguration config)
    {
        var value = config["FOUNDRY_PROJECT_ENDPOINT"]
            ?? throw new InvalidOperationException(
                "FOUNDRY_PROJECT_ENDPOINT missing. Add it via `dotnet user-secrets set FOUNDRY_PROJECT_ENDPOINT <uri>` in the test project.");
        return new Uri(value);
    }

    public static string? GetBingConnectionName(IConfiguration config)
        => config["BING_CONNECTION_NAME"];

    /// <summary>Base URL of the locally-running Function App. Default matches the port pinned
    /// in <c>KanelBrief.Functions/Properties/launchSettings.json</c> (<c>7220</c>).</summary>
    public static string GetSyncBaseUrl(IConfiguration config)
        => config["SYNC_TEST_BASE_URL"] ?? "http://localhost:7220";

    /// <summary>
    /// Bearer token matching <c>SYNC_AUTH_TOKEN</c>. Lookup order:
    /// <list type="number">
    ///   <item><c>SYNC_TEST_AUTH_TOKEN</c> (user secret / env var — for staging overrides)</item>
    ///   <item><c>SYNC_AUTH_TOKEN</c> from <c>KanelBrief.Functions/local.settings.json</c>
    ///     — same file <c>func start</c> uses, so the test always matches the running backend</item>
    ///   <item>Final fallback: <c>dev-token-change-me</c></item>
    /// </list>
    /// </summary>
    public static string GetSyncAuthToken(IConfiguration config)
    {
        var fromConfig = config["SYNC_TEST_AUTH_TOKEN"];
        if (!string.IsNullOrEmpty(fromConfig)) return fromConfig;

        var fromLocalSettings = TryReadLocalSettingsToken();
        if (!string.IsNullOrEmpty(fromLocalSettings)) return fromLocalSettings;

        return "dev-token-change-me";
    }

    private static string? TryReadLocalSettingsToken()
    {
        // AppContext.BaseDirectory is backend/KanelBrief.Functions.Tests/bin/Debug/net9.0 at test time.
        // Walk up to backend/ then into KanelBrief.Functions/local.settings.json.
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "KanelBrief.Functions", "local.settings.json"));

        if (!File.Exists(path)) return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("Values", out var values)
                && values.TryGetProperty("SYNC_AUTH_TOKEN", out var token))
            {
                return token.GetString();
            }
        }
        catch
        {
            // Malformed file — fall through to default.
        }

        return null;
    }
}

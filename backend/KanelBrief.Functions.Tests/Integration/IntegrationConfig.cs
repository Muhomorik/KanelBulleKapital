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
}

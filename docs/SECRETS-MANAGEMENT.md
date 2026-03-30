# Configuration & Secrets Guide

> Complete reference for all environment variables, secrets, and configuration across KanelBulleKapital projects.

## Instructions for AI Agents

**CRITICAL:** When editing this document, AI assistants MUST verify before saving:

- [ ] No real Azure resource names — use `<your-...>` placeholders
- [ ] No real endpoint URLs — use `<your-...>` placeholders
- [ ] No API keys, tokens, or secrets in plain text

## FikaForecast Configuration

### Required

| Secret | Description | Example |
| --- | --- | --- |
| `AzureAIFoundry:ProjectEndpoint` | Foundry project endpoint | `https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>` |

### Optional

| Secret | Description | Example |
| --- | --- | --- |
| `AzureAIFoundry:BingConnectionName` | Bing Grounding connection name | `<your-bing-connection>` |

### Authenticationn

FikaForecast uses **DefaultAzureCredential** — no API key needed.

The recommended approach for local development is **Azure CLI**:

```bash
# Install Azure CLI (one-time)
winget install Microsoft.AzureCLI

# Log in with your Azure account
az login
```

After `az login`, DefaultAzureCredential picks up your credentials automatically.

> **Troubleshooting:** If you get `CredentialUnavailableException`, it means no valid credential was found.
> The most common fixes:
>
> 1. **Install Azure CLI** and run `az login` — this is the most reliable method
> 2. **Re-authenticate in Visual Studio** — Tools → Options → Azure Services Authentication → re-authenticate
> 3. **Check your account** — the Azure account must have access to the Foundry resource
>
> DefaultAzureCredential tries these sources in order: Environment variables → Workload Identity →
> Managed Identity → Visual Studio → VS Code → Azure CLI → Azure PowerShell → Azure Developer CLI.
> For a desktop app, Azure CLI is the simplest.

### Where to Find the Project Endpoint

1. Go to [ai.azure.com](https://ai.azure.com) (Foundry portal)
2. Open your project
3. Overview page → Endpoints and keys → **"Microsoft Foundry project endpoint"**
4. Copy the full URL: `https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>`

### Model Deployment Names

These are configured in `appsettings.json` (not secrets — they're not sensitive):

```json
{
  "FikaForecast": {
    "Models": [
      { "ModelId": "gpt-5.4-mini", "DeploymentName": "gpt-5.4-mini", "DisplayName": "GPT-5.4 Mini" }
    ]
  }
}
```

### Local Development — User Secrets

```bash
# Initialize user secrets for the WPF project
cd FikaForecast/FikaForecast.Wpf
dotnet user-secrets init

# Set Foundry project endpoint
dotnet user-secrets set "AzureAIFoundry:ProjectEndpoint" "https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>"

# Optional: Bing Grounding connection
dotnet user-secrets set "AzureAIFoundry:BingConnectionName" "<your-bing-connection>"
```

Verify secrets are set:

```bash
dotnet user-secrets list
```

## Security Best Practices

### Do

- Use Azure CLI (`az login`) for local development authentication
- Use User Secrets for non-sensitive config like endpoints (never commit to Git)
- Use Azure Key Vault for production (secure, centralized)
- Use DefaultAzureCredential (no API keys in code)
- Grant least privilege access

### Don't

- Commit secrets to Git (check `.gitignore`)
- Hardcode endpoints or API keys in source code
- Share User Secrets files between developers
- Log secret values

---

## Additional Resources

- [.NET User Secrets Documentation](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [DefaultAzureCredential Documentation](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)
- [DefaultAzureCredential Troubleshooting](https://aka.ms/azsdk/net/identity/defaultazurecredential/troubleshoot)
- [Azure CLI Installation](https://learn.microsoft.com/cli/azure/install-azure-cli)
- [Azure Key Vault Overview](https://docs.microsoft.com/azure/key-vault/general/overview)

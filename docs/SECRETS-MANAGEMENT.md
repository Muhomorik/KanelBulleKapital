# Configuration & Secrets Guide

> Complete reference for all environment variables, secrets, and configuration across KanelBulleKapital projects.

## FikaForecast Secrets

### Required Secrets

| Secret | Description | Where Used |
| --- | --- | --- |
| `AzureAIFoundry:Endpoint` | Azure AI Foundry project endpoint | Infrastructure — agent execution |
| `AzureAIFoundry:ApiKey` | Azure AI Foundry API key | Infrastructure — agent authentication |
| `AzureAIFoundry:BingConnectionName` | Bing Grounding connection name | Infrastructure — web search tool |

### Model Deployment Names

These are configured in `appsettings.json` (not secrets — they're not sensitive):

```json
{
  "FikaForecast": {
    "Models": [
      { "ModelId": "gpt-5.1-mini", "DeploymentName": "gpt-51-mini", "DisplayName": "GPT-5.1 Mini" },
      { "ModelId": "gpt-5", "DeploymentName": "gpt-5", "DisplayName": "GPT-5" },
      { "ModelId": "phi-4", "DeploymentName": "phi-4", "DisplayName": "Phi-4" },
      { "ModelId": "deepseek", "DeploymentName": "deepseek", "DisplayName": "DeepSeek" }
    ]
  }
}
```

### Local Development — User Secrets

Use `dotnet user-secrets` for local development. Never commit API keys to source control.

```bash
# Initialize user secrets for the WPF project
cd FikaForecast/FikaForecast.Wpf
dotnet user-secrets init

# Set Azure AI Foundry secrets
dotnet user-secrets set "AzureAIFoundry:Endpoint" "https://your-project.swedencentral.inference.ai.azure.com"
dotnet user-secrets set "AzureAIFoundry:ApiKey" "your-api-key-here"
dotnet user-secrets set "AzureAIFoundry:BingConnectionName" "your-bing-connection"
```

Verify secrets are set:

```bash
dotnet user-secrets list
```

### Production — Azure Key Vault

For production or shared environments, store secrets in Azure Key Vault:

```bash
# Add secrets to Key Vault
az keyvault secret set --vault-name kv-fikaforecast --name "AzureAIFoundry--Endpoint" --value "https://..."
az keyvault secret set --vault-name kv-fikaforecast --name "AzureAIFoundry--ApiKey" --value "..."
az keyvault secret set --vault-name kv-fikaforecast --name "AzureAIFoundry--BingConnectionName" --value "..."
```

> Note: Key Vault uses `--` as separator instead of `:` for nested configuration keys.

## Security Best Practices

### Do

- Use User Secrets for local development (never commit API keys)
- Use Azure Key Vault for production (secure, centralized)
- Rotate API keys regularly (every 90 days recommended)
- Use Managed Identity (no credentials in code)
- Grant least privilege access to secrets

### Don't

- Commit secrets to Git (check `.gitignore`)
- Hardcode API keys in source code
- Share User Secrets files between developers
- Use production secrets in local development
- Log secret values (ensure logging doesn't expose sensitive data)
- Put secrets in `NEXT_PUBLIC_` environment variables

---

## Additional Resources

- [.NET User Secrets Documentation](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Key Vault Overview](https://docs.microsoft.com/azure/key-vault/general/overview)
- [Managed Identity Best Practices](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/best-practice-recommendations)
- [GitHub Secrets Documentation](https://docs.github.com/actions/security-guides/encrypted-secrets)
- [Next.js Environment Variables](https://nextjs.org/docs/basic-features/environment-variables)

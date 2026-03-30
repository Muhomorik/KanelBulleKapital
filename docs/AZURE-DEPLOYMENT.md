# Azure Deployment Guide

Complete guide for deploying FikaForecast resources to Azure.

## Instructions for AI Agents

**CRITICAL:** When editing this document, AI assistants MUST verify before saving:

- [ ] No real Azure resource names — use `<your-...>` placeholders
- [ ] No real endpoint URLs — use `<your-...>` placeholders
- [ ] No API keys, tokens, or secrets in plain text

## Resource Group and Cost Management

All resources live in a dedicated resource group: `<your-resource-group>`

### Budget Alert

- **Threshold:** $40/month
- **Alerts:** Email at 50% ($20) and 80% ($32)
- **Alert recipients:** Resource group owner

### Estimated Monthly Costs

| Resource | Pricing | Estimated Cost (light use) | Estimated Cost (daily use) |
| --- | --- | --- | --- |
| **AI Foundry resource** | Free (no idle cost) | $0 | $0 |
| **gpt-5.4-mini** (Global Standard) | Pay per token | ~$0.01/brief | ~$0.30/month |
| **gpt-5.4** (Global Standard) | Pay per token | ~$0.05-0.10/brief | ~$1.50-3.00/month |
| **gpt-5.4-nano** (Global Standard) | Pay per token | ~$0.005/brief | ~$0.15/month |
| **Bing Grounding** (S tier) | $14 per 1,000 transactions | ~$1.40-2.80/month | ~$4.20-8.40/month |
| **Storage account** | Minimal | ~$0.01/month | ~$0.01/month |
| **Total** | | **~$2-4/month** | **~$6-12/month** |

> Each brief run triggers ~5-10 Bing search transactions. Heavy comparison testing (all models daily) could reach ~$15-20/month for Bing alone.

### Budget Setup (Portal)

1. Open `<your-resource-group>` in Azure Portal
2. Left menu → **Cost Management** → **Budgets** → **+ Add**
3. Name: `<your-budget-name>`, Amount: `40`, Reset: `Monthly`
4. Add alert at 50% and 80%, enter your email

## Microsoft Foundry (Azure AI Foundry)

> In the Azure Portal, search for **"Microsoft Foundry"** (rebranded from Azure AI Foundry).

### Resource Setup (Portal)

1. Search **"Microsoft Foundry"** in Azure Portal
2. Click **Create a resource**
3. Fill in:
   - **Resource group**: `<your-resource-group>`
   - **Name**: `<your-ai-resource>`
   - **Region**: `Sweden Central`
4. Leave Storage, Network, Identity, Encryption as defaults
5. **Review + create** → **Create**
6. Click **Go to resource** → **Go to Foundry portal**

### Project

A default project is created automatically: `<your-project>`

### Model Deployments

In the Foundry portal → **Model catalog** → search and deploy:

| Model | Deployment Name | Deployment Type | Status |
| --- | --- | --- | --- |
| gpt-4.1 | gpt-4.1 | Global Standard | Deployed |
| gpt-5.4-mini | gpt-5.4-mini | Global Standard | TODO (needs SDK migration to Azure.AI.Projects for Bing Grounding) |
| gpt-5.4 | gpt-5.4 | Global Standard | TODO |
| gpt-5.4-nano | gpt-5.4-nano | Global Standard | TODO |
| DeepSeek | deepseek | Serverless | TODO |

> **Global Standard** = pay per token, no idle cost. Safe to leave deployed.

### Bing Grounding

The News Brief Agent uses Bing Grounding to search for real-time news. LLMs have a knowledge cutoff — without Bing, the model cannot access current events.

**Step 1 — Create Bing Search resource (Azure Portal):**

1. Search **"Grounding with Bing Search"** in Azure Portal
2. Click **Create**
3. Fill in:
   - **Resource group**: `<your-resource-group>`
   - **Name**: `<your-bing-resource>`
   - **Pricing tier**: S ($14 per 1,000 transactions)
4. Check the terms checkbox → **Review + create** → **Create**

**Step 2 — Connect to Foundry (Foundry Portal):**

1. Go to the Foundry portal → **Management center** (bottom-left)
2. Go to **Connected resources** → **+ New connection**
3. Select **Grounding with Bing Search**
4. Pick the Bing resource you created
5. Note the **connection name**

**Step 3 — Save connection name:**

```bash
cd FikaForecast/FikaForecast.Wpf
dotnet user-secrets set "AzureAIFoundry:BingConnectionName" "<connection-name>"
```

### Authentication and Endpoints

FikaForecast uses **DefaultAzureCredential** — no API keys needed. The recommended local setup is Azure CLI:

```bash
# Install Azure CLI (one-time)
winget install Microsoft.AzureCLI

# Log in with your Azure account (same account as Azure Portal)
az login
```

After setup, save the project endpoint as a user secret (see [SECRETS-MANAGEMENT.md](SECRETS-MANAGEMENT.md)):

| Value | Where to find it |
| --- | --- |
| Project endpoint | Foundry portal → project overview → "Microsoft Foundry project endpoint": `https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>` |
| Bing connection | Management center → Connected resources → connection name |

```bash
cd FikaForecast/FikaForecast.Wpf
dotnet user-secrets set "AzureAIFoundry:ProjectEndpoint" "https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>"
dotnet user-secrets set "AzureAIFoundry:BingConnectionName" "<your-bing-connection>"
```

## Existing Services (Shared Backend)

These services are already deployed and shared with [SemanticKernel-FundDocsQnA](https://github.com/Muhomorik/SemanticKernel-FundDocsQnA-dotnet-nextjs):

- **Azure App Service F1** (free tier) -- Backend API
- **Azure Static Web Apps** -- Frontend
- **Azure Key Vault** -- Secrets
- **Application Insights** -- Monitoring

## Additional Resources

- [Microsoft Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Key Vault Quickstart](https://docs.microsoft.com/azure/key-vault/general/quick-create-cli)
